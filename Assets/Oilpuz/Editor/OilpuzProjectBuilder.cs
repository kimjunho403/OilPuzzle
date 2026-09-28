#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Oilpuz.Editor
{
    public static class OilpuzProjectBuilder
    {
        const string Root="Assets/Oilpuz/";
        const string ScenePath=Root+"Scenes/MalaPuzzle.unity";
        [MenuItem("Oilpuz/Build or refresh mala scene")]
        public static void Setup()
        {
            AssetDatabase.Refresh();
            foreach(string file in Directory.GetFiles(Root+"Art/Mala","*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(file.Replace('\\','/'));
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
                importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.maxTextureSize=file.Contains("Table")||file.Contains("Bowl")?2048:1024;
                var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);importer.SaveAndReimport();
            }
            var skin=Asset<SoupSkin>(Root+"Data/Mala.asset");skin.skinId="mala";skin.displayName="마라탕";
            skin.table=Sprite("Table");skin.bowlBroth=Sprite("BowlBroth");skin.mushroom=Sprite("Shiitake");skin.fishBall=Sprite("FishBall");skin.garnish=Sprite("BokChoy");
            skin.font=AssetDatabase.LoadAssetAtPath<Font>(Root+"Fonts/NotoSansCJKkr-Regular.otf");EditorUtility.SetDirty(skin);
            var stages=MakeStages();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));cameraObject.tag="MainCamera";
            var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.96f,.93f,.85f);
            var game=new GameObject("Oilpuz — Mala puzzle").AddComponent<OilGameController>();game.skin=skin;game.stages=stages;
            game.densityShader=Shader.Find("Oilpuz/OilDensity");game.surfaceShader=Shader.Find("Oilpuz/OilSurface");
            if(game.densityShader==null||game.surfaceShader==null)throw new Exception("Oil shaders not imported.");
            game.BuildPresentation();
            SavePreview(game);
            EditorSceneManager.SaveScene(scene,ScenePath);
            var scenes=new List<EditorBuildSettingsScene>{new EditorBuildSettingsScene(ScenePath,true)};
            scenes.AddRange(EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath));EditorBuildSettings.scenes=scenes.ToArray();
            PlayerSettings.productName="Oilpuz";PlayerSettings.companyName="Oilpuz";
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;PlayerSettings.defaultIsNativeResolution=false;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
            AssetDatabase.SaveAssets();
            Debug.Log("OILPUZ_SETUP_OK "+ScenePath);
        }
        static void SavePreview(OilGameController game)
        {
            Directory.CreateDirectory(Root+"Art/Generated");Directory.CreateDirectory(Root+"Materials");
            var old=RenderTexture.active;RenderTexture.active=game.OilDrawing.Texture;
            var texture=new Texture2D(512,512,TextureFormat.RGBAFloat,false,true);texture.ReadPixels(new Rect(0,0,512,512),0,0);texture.Apply();
            // Density can exceed 1, so keep an EXR preview for the authored edit-time scene.
            string path=Root+"Art/Generated/PreviewDensity.exr";File.WriteAllBytes(path,texture.EncodeToEXR(Texture2D.EXRFlags.CompressZIP));UnityEngine.Object.DestroyImmediate(texture);RenderTexture.active=old;
            AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Materials/MalaOil.mat");
            if(material==null){material=new Material(game.surfaceShader);AssetDatabase.CreateAsset(material,Root+"Materials/MalaOil.mat");}
            material.CopyPropertiesFromMaterial(game.OilDrawing.Surface);material.hideFlags=HideFlags.None;EditorUtility.SetDirty(material);
            game.OilImage.texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);material.mainTexture=game.OilImage.texture;game.OilImage.material=material;
        }
        static Sprite Sprite(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Mala/"+name+".png");
        static T Asset<T>(string path) where T:ScriptableObject
        {var asset=AssetDatabase.LoadAssetAtPath<T>(path);if(asset==null){asset=ScriptableObject.CreateInstance<T>();AssetDatabase.CreateAsset(asset,path);}return asset;}
        static StageDefinition Stage(int index,string title,string hint,OilSeed[] seeds,FoodObstacle[] obstacles,int splits,Vector2 goal,float goalRadius,float current=0,bool gate=false,float gateX=0,float gateWidth=.24f)
        {
            var s=Asset<StageDefinition>(Root+$"Data/Stage{index:00}.asset");s.title=title;s.hint=hint;s.seeds=seeds;s.obstacles=obstacles;s.splitLimit=splits;s.goal=goal;s.goalRadius=goalRadius;s.current=current;s.hasGate=gate;s.gateX=gateX;s.gateY=0;s.gateWidth=gateWidth;EditorUtility.SetDirty(s);return s;
        }
        static StageDefinition[] MakeStages()=>new[]{
            Stage(1,"첫 숟가락","기름 가장자리를 집고, 천천히 원 안에 모아요.",new[]{new OilSeed(-.43f,.33f,2),new OilSeed(.43f,.28f,2),new OilSeed(-.35f,-.30f)},new[]{new FoodObstacle(0,.06f,.12f),new FoodObstacle(.65f,-.33f,.10f,true)},2,new Vector2(.12f,-.49f),.37f),
            Stage(2,"재료 사이로","잡은 곳이 먼저, 뒤쪽은 천천히 따라와요.",new[]{new OilSeed(-.42f,.42f,2),new OilSeed(.43f,.42f,2),new OilSeed(.55f,-.25f),new OilSeed(-.37f,-.40f)},new[]{new FoodObstacle(0,.04f,.23f),new FoodObstacle(-.60f,-.16f,.13f,true)},2,new Vector2(.07f,-.55f),.32f),
            Stage(3,"모으기 전에","작을 때 가운데 문을 건너, 아래에서 합쳐요.",new[]{new OilSeed(-.50f,.43f),new OilSeed(-.20f,.66f),new OilSeed(.20f,.65f),new OilSeed(.51f,.43f),new OilSeed(0,.24f)},new[]{new FoodObstacle(-.45f,0,.33f),new FoodObstacle(.45f,0,.33f),new FoodObstacle(-.85f,0,.13f,true),new FoodObstacle(.85f,0,.13f,true)},1,new Vector2(0,-.55f),.33f,0,true,0,.24f),
            Stage(4,"국물의 흐름","물살을 따라 작은 방울부터 왼쪽 문으로.",new[]{new OilSeed(-.48f,.42f),new OilSeed(-.22f,.67f),new OilSeed(.08f,.71f),new OilSeed(.41f,.58f),new OilSeed(.60f,.40f),new OilSeed(-.06f,.33f)},new[]{new FoodObstacle(-.62f,0,.345f),new FoodObstacle(.325f,0,.37f),new FoodObstacle(.86f,0,.19f,true)},1,new Vector2(.02f,-.56f),.33f,.22f,true,-.16f,.23f),
            Stage(5,"흔들림 없이","한 방울씩 가운데로. 갈라짐 없이 모아보세요.",new[]{new OilSeed(-.49f,.46f),new OilSeed(-.20f,.68f),new OilSeed(.10f,.70f),new OilSeed(.44f,.50f),new OilSeed(.63f,.43f),new OilSeed(-.15f,.39f),new OilSeed(.12f,.35f)},new[]{new FoodObstacle(-.44f,0,.35f),new FoodObstacle(.44f,0,.35f),new FoodObstacle(-.84f,0,.14f,true),new FoodObstacle(.84f,0,.14f,true)},0,new Vector2(0,-.56f),.33f,.3f,true,0,.18f)
        };

        [MenuItem("Oilpuz/Validate simulation and assets")]
        public static void Validate()
        {
            var report=new List<string>();
            void Check(bool condition,string description){if(!condition)throw new Exception("OILPUZ_VALIDATION_FAILED: "+description);report.Add(description);Debug.Log("PASS "+description);}
            StageDefinition Empty(params OilSeed[] seeds){var s=ScriptableObject.CreateInstance<StageDefinition>();s.seeds=seeds;s.goal=new Vector2(0,-.5f);s.goalRadius=.35f;s.splitLimit=20;return s;}
            var stage=Empty(new OilSeed(-.15f,.2f,2));var sim=new OilSimulation();sim.Reset(stage);float area=sim.TotalArea;int count=sim.Particles.Count;
            int lead=0,tail=0;for(int i=0;i<count;i++){if(sim.Particles[i].position.x>sim.Particles[lead].position.x)lead=i;if(sim.Particles[i].position.x<sim.Particles[tail].position.x)tail=i;}
            Vector2 start=sim.Particles[lead].position,tailStart=sim.Particles[tail].position;
            Check(sim.Grab(start+new Vector2(.008f,.003f)),"Actual surface point is grabbable");
            Check(Vector2.Distance(start,sim.Particles[lead].position)<.000001f,"Pointer down does not snap oil to its center");
            int grabbed=sim.GrabbedParticle;
            for(int i=0;i<20;i++){sim.Move(start+new Vector2(.008f+i*.004f,.003f));sim.Step();}
            float frontTravel=sim.Particles[lead].position.x-start.x,backTravel=sim.Particles[tail].position.x-tailStart.x;
            Check(frontTravel>backTravel*1.5f && frontTravel>.01f,$"Grabbed edge leads the rear: front={frontTravel:F4}, rear={backTravel:F4}");
            Check(sim.GrabbedParticle==grabbed,"Material handle remains attached to same particle");
            Check(sim.SplitCount==0,"Slow local drag stays connected");sim.Release();
            Check(!sim.Dragging,"Pointer release clears material handle");
            for(int i=0;i<120;i++)sim.Step();
            Check(sim.Particles.Count==count && Mathf.Abs(sim.TotalArea-area)<.000001f,"Relaxation conserves material area");
            sim.Reset(stage);start=sim.Particles[lead].position;sim.Grab(start);
            for(int i=0;i<90;i++){sim.Move(start+Vector2.right*i*.035f);sim.Step();}
            Check(sim.SplitCount>0,"Fast pull tears an actual connected neck");
            Check(sim.Particles.Count==count && Mathf.Abs(sim.TotalArea-area)<.000001f,"Tearing conserves all oil particles and area");
            stage.splitLimit=0;sim.Reset(stage);sim.Grab(sim.Particles[lead].position);start=sim.Particles[lead].position;
            for(int i=0;i<90;i++){sim.Move(start+Vector2.right*i*.035f);sim.Step();}
            Check(sim.Failed,"Exceeding a zero split budget fails");float frozen=sim.Time;sim.Step();Check(sim.Time==frozen,"Failed simulation freezes");
            sim.Reset(stage);Check(!sim.Failed&&sim.SplitCount==0,"Restart clears failure and split budget");
            var mergeStage=Empty(new OilSeed(-.13f,.22f),new OilSeed(.13f,.22f));sim.Reset(mergeStage);
            sim.Grab(sim.Particles[0].position);start=sim.Particles[0].position;
            for(int i=0;i<240;i++){sim.Move(start+Vector2.right*Mathf.Min(.25f,i*.0015f));sim.Step();}
            Check(sim.GroupCount==1,"Two oil groups merge through material contact");
            Check(sim.GrabbedParticle==0,"Merge preserves grabbed particle identity");
            sim.Release();for(int i=0;i<180;i++)sim.Step();Check(!sim.Won,"One group outside destination does not win");
            var goalStage=Empty(new OilSeed(0,-.5f));sim.Reset(goalStage);for(int i=0;i<180;i++)sim.Step();Check(sim.Won,"One released group fully inside destination wins");
            var collisionStage=Empty(new OilSeed(-.36f,0));collisionStage.obstacles=new[]{new FoodObstacle(0,0,.18f)};sim.Reset(collisionStage);sim.Grab(sim.Particles[0].position);
            for(int i=0;i<90;i++)
            {
                sim.Move(new Vector2(.6f,0));sim.Step();
                if(sim.Particles.Any(p=>p.position.magnitude<.18f+OilSimulation.ParticleRadius-.002f))throw new Exception("Material penetrated food");
            }
            Check(sim.Particles.All(p=>p.position.magnitude>=.18f+OilSimulation.ParticleRadius-.002f),"Local grab constraint cannot flick material through an ingredient");
            for(int i=1;i<=5;i++)
            {
                var s=AssetDatabase.LoadAssetAtPath<StageDefinition>(Root+$"Data/Stage{i:00}.asset");sim.Reset(s);
                bool fits=sim.Particles.All(p=>p.position.magnitude+OilSimulation.ParticleRadius<OilSimulation.BowlRadius+.001f&&s.obstacles.All(o=>Vector2.Distance(p.position,o.position)+.002f>=o.radius+OilSimulation.ParticleRadius));
                Check(fits,$"Stage {i} starts with legal material positions");
                int groups=sim.GroupCount;for(int n=0;n<120;n++)sim.Step();Check(!sim.Failed&&sim.GroupCount==groups,$"Stage {i} has stable separated initial groups");
            }
            foreach(string shaderPath in new[]{Root+"Shaders/OilDensity.shader",Root+"Shaders/OilSurface.shader"})
            {
                var shader=AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
                Check(!ShaderUtil.GetShaderMessages(shader).Any(m=>m.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error),"Shader compiles: "+shader.name);
            }
            Check(AssetDatabase.LoadAssetAtPath<SoupSkin>(Root+"Data/Mala.asset").font!=null,"Korean font is referenced by skin");
            File.WriteAllText("Docs/Validation-results.txt",DateTime.Now.ToString("s")+"\n"+string.Join("\n",report));
            Debug.Log("OILPUZ_VALIDATION_OK "+report.Count+" checks");
        }
        public static void SetupAndValidate(){Setup();Validate();}
        public static void SetupValidateAndBuild(){Setup();Validate();BuildWindows();}
        // Refresh visuals without replacing authored stage data or the template scene.
        public static void RefreshCasualAndValidate()
        {
            AssetDatabase.Refresh();
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var game=UnityEngine.Object.FindFirstObjectByType<OilGameController>();
            game.BuildPresentation();SavePreview(game);
            EditorSceneManager.SaveScene(scene,ScenePath);
            AssetDatabase.SaveAssets();Validate();ValidateFluidRelaxation();
        }
        static void ValidateFluidRelaxation()
        {
            var stage=ScriptableObject.CreateInstance<StageDefinition>();
            stage.seeds=new[]{new OilSeed(-.25f,.25f,2)};stage.obstacles=Array.Empty<FoodObstacle>();
            stage.goal=new Vector2(0,-.6f);stage.goalRadius=.2f;stage.splitLimit=20;
            var sim=new OilSimulation();sim.Reset(stage);
            int count=sim.Particles.Count;
            int lead=0;for(int i=1;i<count;i++)if(sim.Particles[i].position.x>sim.Particles[lead].position.x)lead=i;
            Vector2 start=sim.Particles[lead].position;
            sim.Grab(start);
            for(int i=0;i<120;i++){sim.Move(start+new Vector2(.28f,.07f)*(i/120f));sim.Step();}
            if(sim.SplitCount!=0||sim.GroupCount!=1||sim.GrabbedParticle!=lead)throw new Exception("Slow viscous drag broke material attachment.");
            int relaxed=sim.RelaxedLinks;
            sim.Release();float earlySpeed=sim.Particles.Sum(p=>p.velocity.sqrMagnitude);
            for(int i=0;i<240;i++)sim.Step();
            float settledSpeed=sim.Particles.Sum(p=>p.velocity.sqrMagnitude);
            if(settledSpeed>Mathf.Max(.002f,earlySpeed*.2f))throw new Exception("Oil retains excessive elastic motion after release.");
            if(sim.Particles.Count!=count||sim.GroupCount!=1)throw new Exception("Relaxation lost liquid material.");
            if(relaxed==0)throw new Exception("Liquid never exchanged its original spring lattice.");
            string report=$"OILPUZ_FLUID_OK: local handle retained, slow drag connected, neighbour exchanges={relaxed}, release energy {earlySpeed:F5} -> {settledSpeed:F5}, material conserved.";
            File.WriteAllText("Docs/Fluid-validation.txt",DateTime.Now.ToString("s")+"\n"+report);Debug.Log(report);
            UnityEngine.Object.DestroyImmediate(stage);
        }
        public static void BuildCasualPreview(){RefreshCasualAndValidate();BuildWindows();}
        [MenuItem("Oilpuz/Build Android APK")]
        public static void BuildAndroid()
        {
            PlayerSettings.productName="Oilpuz";PlayerSettings.bundleVersion="0.2.0";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.kimjunho.oilpuz");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel23;
            PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel35;
            PlayerSettings.Android.bundleVersionCode=2;
            PlayerSettings.Android.useCustomKeystore=false;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
            EditorUserBuildSettings.buildAppBundle=false;
            EditorUserBuildSettings.exportAsGoogleAndroidProject=false;
            Directory.CreateDirectory("Builds/Android");
            AssetDatabase.SaveAssets();
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Android/Oilpuz-v0.2.0-arm64.apk",target=BuildTarget.Android,options=BuildOptions.None});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Android APK build failed: "+result.summary.result);
            Debug.Log("OILPUZ_ANDROID_OK "+result.summary.totalSize);
        }
        [MenuItem("Oilpuz/Build Windows preview")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/Windows/Oilpuz.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
            if(result.summary.result!=BuildResult.Succeeded)throw new Exception("Windows preview build failed: "+result.summary.result);
            Debug.Log("OILPUZ_BUILD_OK "+result.summary.totalSize);
        }
    }
}
#endif
