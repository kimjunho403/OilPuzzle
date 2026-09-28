using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Oilpuz
{
    public sealed class OilGameController : MonoBehaviour
    {
        public SoupSkin skin;
        public StageDefinition[] stages;
        public Shader densityShader, surfaceShader;
        public int initialStage;
        public OilSimulation Simulation { get; private set; }
        public bool CanPlay => !paused && !dialogOpen && Simulation != null && !Simulation.Won && !Simulation.Failed;
        public RawImage OilImage { get; private set; }
        public OilRenderer OilDrawing { get; private set; }
        RectTransform root, safeArea, board, foodRoot;
        Text stageTitle, skinTitle, countText, splitText, hintText, tensionText, goalText;
        UICard tensionFill;
        UIRing goalRing, progressRing, touchRing;
        OilInputSurface input;
        GameObject overlay;
        Text modalTitle, modalBody, modalPrimaryText, modalSecondaryText;
        Button modalPrimary, modalSecondary;
        Button[] stageButtons;
        bool paused, dialogOpen, resultShown;
        int stageIndex, previousMerges, previousSplits, frame;
        float accumulator, feedbackUntil;
        string capturePath;
        bool captureDemo;
        Vector2 demoPointer;
        public StageDefinition CurrentStage => stages[stageIndex];

        void Awake()
        {
            if(skin==null || stages==null || stages.Length==0) { Debug.LogError("Oilpuz requires a SoupSkin and stages.");enabled=false;return; }
            Application.targetFrameRate=60;Screen.orientation=ScreenOrientation.Portrait;
            capturePath=Argument("--oil-capture");captureDemo=Array.IndexOf(Environment.GetCommandLineArgs(),"--oil-drag-demo")>=0;
            if(int.TryParse(Argument("--oil-stage"),out int stage))initialStage=Mathf.Clamp(stage,0,stages.Length-1);
            BuildPresentation();
        }
        public void BuildPresentation()
        {
            var old=transform.Find("Oilpuz UI");
            if(old!=null) { old.gameObject.SetActive(false);if(Application.isPlaying)Destroy(old.gameObject);else DestroyImmediate(old.gameObject); }
            OilDrawing?.Dispose();
            Simulation=new OilSimulation();stageIndex=Mathf.Clamp(initialStage,0,stages.Length-1);Simulation.Reset(stages[stageIndex]);
            OilDrawing=new OilRenderer(densityShader,surfaceShader,skin);
            var ui=new GameObject("Oilpuz UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));ui.transform.SetParent(transform,false);
            var canvas=ui.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=ui.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=.5f;
            var backdrop=Image("Table",ui.transform,skin.table,Vector2.zero,new Vector2(1080,1920));Stretch(backdrop.rectTransform);backdrop.preserveAspect=false;
            safeArea=Rect("Safe area",ui.transform,Vector2.zero,Vector2.zero);Stretch(safeArea);
            root=Rect("Portrait composition",safeArea,Vector2.zero,new Vector2(1080,1920));
            Label("Brand","Oilpuz",root,new Vector2(0,839),new Vector2(560,116),88,skin.ink,FontStyle.Bold);
            Label("Tagline","기름이 이어주는 작은 식탁",root,new Vector2(0,770),new Vector2(640,52),23,Muted);
            Button("Pause","Ⅱ",root,new Vector2(449,837),new Vector2(88,88),()=>TogglePause(),37);
            Button("Help","?",root,new Vector2(-449,837),new Vector2(88,88),ShowHelp,32);
            stageTitle=Label("Stage title","",root,new Vector2(0,671),new Vector2(980,81),44,skin.ink,FontStyle.Bold);
            skinTitle=Label("Soup name",skin.displayName,root,new Vector2(0,609),new Vector2(500,50),29,new Color(.58f,.24f,.12f));
            Card("Drop card",root,new Vector2(-211,515),new Vector2(396,100),new Color(1,.97f,.88f,.83f));
            Card("Split card",root,new Vector2(211,515),new Vector2(396,100),new Color(1,.97f,.88f,.83f));
            Label("Drop label","남은 방울",root,new Vector2(-273,515),new Vector2(220,64),29,skin.ink);
            countText=Label("Drop count","",root,new Vector2(-101,515),new Vector2(90,70),45,skin.ink,FontStyle.Bold);
            Label("Split label","갈라짐",root,new Vector2(123,515),new Vector2(171,64),29,skin.ink);
            splitText=Label("Split count","",root,new Vector2(281,515),new Vector2(209,70),41,skin.ink,FontStyle.Bold);
            Image("Bowl and broth",root,skin.bowlBroth,new Vector2(0,-49),new Vector2(1080,1080));
            board=Rect("Soup coordinates",root,new Vector2(0,-49),new Vector2(800,800));
            var garnish=Image("Decorative bok choy",board,skin.garnish,new Vector2(-286,-211),new Vector2(164,181));garnish.rectTransform.localRotation=Quaternion.Euler(0,0,-40);
            var garnish2=Image("Decorative bok choy 2",board,skin.garnish,new Vector2(281,208),new Vector2(131,150));garnish2.rectTransform.localRotation=Quaternion.Euler(0,0,143);
            goalRing=Ring("Goal",board,Vector2.zero,Vector2.one*230,skin.accent,3,32);
            progressRing=Ring("Goal progress",board,Vector2.zero,Vector2.one*235,skin.accent,5,0);progressRing.progress=0;
            goalText=Label("Goal caption","여기에 모아요",board,Vector2.zero,new Vector2(250,35),20,new Color(.83f,.91f,.64f));
            var oil=Rect("Dynamic oil",board,Vector2.zero,new Vector2(800,800));OilImage=oil.gameObject.AddComponent<RawImage>();OilImage.texture=OilDrawing.Texture;OilImage.material=OilDrawing.Surface;OilImage.raycastTarget=false;
            foodRoot=Rect("Ingredient obstacles",board,Vector2.zero,new Vector2(800,800));
            touchRing=Ring("Grabbed material",board,Vector2.zero,new Vector2(37,37),new Color(1,.96f,.8f,.88f),2.4f,0);touchRing.gameObject.SetActive(false);
            var touch=Rect("Touch surface",board,Vector2.zero,new Vector2(800,800));var receiver=touch.gameObject.AddComponent<Image>();receiver.color=Color.clear;receiver.raycastTarget=true;
            input=touch.gameObject.AddComponent<OilInputSurface>();input.controller=this;
            Label("Tension title","손끝의 장력",root,new Vector2(-316,-654),new Vector2(240,50),26,skin.ink);
            Card("Tension track",root,new Vector2(9,-655),new Vector2(322,16),new Color(.63f,.56f,.44f,.3f),8);
            tensionFill=Card("Tension fill",root,new Vector2(-151,-655),new Vector2(0,16),skin.accent,8);tensionFill.rectTransform.pivot=new Vector2(0,.5f);
            tensionText=Label("Tension word","고요해요",root,new Vector2(305,-654),new Vector2(220,50),25,Muted);
            hintText=Label("Hint","",root,new Vector2(0,-713),new Vector2(960,80),25,Muted);
            Button("Restart","↻  다시 담기",root,new Vector2(0,-805),new Vector2(412,92),()=>LoadStage(stageIndex),34);
            stageButtons=new Button[stages.Length];
            for(int i=0;i<stages.Length;i++){int index=i;stageButtons[i]=Button("Stage "+(i+1),(i+1).ToString("00"),root,new Vector2((i-(stages.Length-1)*.5f)*126,-910),new Vector2(83,77),()=>LoadStage(index),27);}
            BuildModal();
            if(FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("Oilpuz EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            Layout();RefreshStageView();OilDrawing.Draw(Simulation);
        }
        Color Muted => new Color(.48f,.43f,.33f);
        static string Argument(string key) {var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,key);return i>=0&&i+1<args.Length?args[i+1]:null;}
        public void LoadStage(int index)
        {
            if(index<0||index>=stages.Length)return;
            input?.Cancel();stageIndex=index;Simulation.Reset(CurrentStage);paused=dialogOpen=resultShown=false;accumulator=0;feedbackUntil=0;previousMerges=previousSplits=0;
            if(overlay!=null)overlay.SetActive(false);RefreshStageView();
        }
        void RefreshStageView()
        {
            stageTitle.text=$"{stageIndex+1:00}  {CurrentStage.title}";
            skinTitle.text=skin.displayName+(CurrentStage.current>0?"  ·  물살 있음":"  ·  잔잔한 국물");
            hintText.text=CurrentStage.hint;
            for(int i=foodRoot.childCount-1;i>=0;i--){var child=foodRoot.GetChild(i).gameObject;child.SetActive(false);if(Application.isPlaying)Destroy(child);else DestroyImmediate(child);}
            for(int i=0;i<CurrentStage.obstacles.Length;i++)
            {
                var o=CurrentStage.obstacles[i];float width=o.radius*800/(o.fishBall?.78f:.82f);
                var image=Image("Ingredient "+i,foodRoot,o.fishBall?skin.fishBall:skin.mushroom,o.position*400,Vector2.one*width);image.rectTransform.localRotation=Quaternion.Euler(0,0,i*39);
            }
            goalRing.rectTransform.anchoredPosition=CurrentStage.goal*400;goalRing.rectTransform.sizeDelta=Vector2.one*CurrentStage.goalRadius*800;
            progressRing.rectTransform.anchoredPosition=CurrentStage.goal*400;progressRing.rectTransform.sizeDelta=Vector2.one*(CurrentStage.goalRadius*800+10);
            goalText.rectTransform.anchoredPosition=CurrentStage.goal*400+Vector2.down*(CurrentStage.goalRadius*400+27);
            for(int i=0;i<stageButtons.Length;i++)
            {
                var card=stageButtons[i].GetComponent<UICard>();card.color=i==stageIndex?skin.accent:new Color(.99f,.96f,.88f,.87f);
                var label=stageButtons[i].GetComponentInChildren<Text>();bool complete=PlayerPrefs.GetInt("Oilpuz.mala.v1.stage."+i,0)>0;
                label.text=(i+1).ToString("00")+(complete?" ·":"");label.color=i==stageIndex?Color.white:Muted;
            }
            UpdateHud();
        }
        void BuildModal()
        {
            var shade=Card("Modal",root,Vector2.zero,new Vector2(1080,1920),new Color(.12f,.1f,.06f,.48f),0);shade.raycastTarget=true;overlay=shade.gameObject;
            Card("Modal card",shade.transform,Vector2.zero,new Vector2(874,676),new Color(.98f,.96f,.88f),38);
            Label("Modal mark","Oilpuz",shade.transform,new Vector2(0,232),new Vector2(700,80),46,skin.accent,FontStyle.Bold);
            modalTitle=Label("Modal title","",shade.transform,new Vector2(0,130),new Vector2(794,76),39,skin.ink,FontStyle.Bold);
            modalBody=Label("Modal body","",shade.transform,new Vector2(0,-9),new Vector2(770,170),27,Muted);
            modalPrimary=Button("Continue","",shade.transform,new Vector2(0,-181),new Vector2(680,93),null,31);modalPrimary.GetComponent<UICard>().color=skin.accent;modalPrimaryText=modalPrimary.GetComponentInChildren<Text>();modalPrimaryText.color=Color.white;
            modalSecondary=Button("Secondary","",shade.transform,new Vector2(0,-278),new Vector2(650,67),null,25);modalSecondary.GetComponent<UICard>().color=Color.clear;modalSecondaryText=modalSecondary.GetComponentInChildren<Text>();
            overlay.SetActive(false);
        }
        void ShowModal(string title,string body,string primary,Action action,string secondary,Action secondaryAction)
        {
            input.Cancel();dialogOpen=true;overlay.SetActive(true);modalTitle.text=title;modalBody.text=body;modalPrimaryText.text=primary;modalSecondaryText.text=secondary;
            modalPrimary.onClick.RemoveAllListeners();modalSecondary.onClick.RemoveAllListeners();
            modalPrimary.onClick.AddListener(()=>action());modalSecondary.onClick.AddListener(()=>secondaryAction());
        }
        void CloseModal() {dialogOpen=false;paused=false;overlay.SetActive(false);accumulator=0;}
        public void TogglePause()
        {
            if(Simulation.Won||Simulation.Failed)return;
            if(paused){CloseModal();return;}paused=true;
            ShowModal("잠시, 식혀가요.","방울들은 이 자리에서 기다릴게요.","계속하기",CloseModal,"처음부터 다시",()=>LoadStage(stageIndex));
        }
        void ShowHelp()
        {
            ShowModal("가장자리를 집어보세요.","누른 곳이 먼저 움직이고, 뒷부분이 따라와요.\n빠르게 당기면 목이 늘어나다 갈라져요.\n작을 때 통로를 건너 목표 원에서 합쳐보세요.","시작할게요",CloseModal,"현재 스테이지 다시 담기",()=>LoadStage(stageIndex));
        }
        void Layout()
        {
            if(root==null||Screen.width<=0||Screen.height<=0)return;
            var safe=Screen.safeArea;if(safe.width<1||safe.height<1)safe=new Rect(0,0,Screen.width,Screen.height);safeArea.anchorMin=new Vector2(safe.xMin/Screen.width,safe.yMin/Screen.height);safeArea.anchorMax=new Vector2(safe.xMax/Screen.width,safe.yMax/Screen.height);
            Canvas.ForceUpdateCanvases();float scale=Mathf.Min(safeArea.rect.width/1080,safeArea.rect.height/1920);root.localScale=Vector3.one*scale;
        }
        void Update()
        {
            if(Simulation==null)return;
            Layout();frame++;
            float frameDelta=captureDemo?1f/60f:UnityEngine.Time.deltaTime;
            if(captureDemo && frame==30)
            {
                int group=Simulation.Particles[0].group;int pick=0;
                for(int i=0;i<Simulation.Particles.Count;i++)if(Simulation.Particles[i].group==group&&Simulation.Particles[i].position.x>Simulation.Particles[pick].position.x)pick=i;
                demoPointer=Simulation.Particles[pick].position;input.OnPointerDown(DemoPointerEvent(demoPointer));
            }
            if(captureDemo&&frame>30&&frame<110){demoPointer+=new Vector2(.7f,.22f)*frameDelta;input.OnDrag(DemoPointerEvent(demoPointer));}
            if(CanPlay)
            {
                accumulator+=Mathf.Min(frameDelta,.1f);
                while(accumulator>=OilSimulation.FixedDelta){Simulation.Step();accumulator-=OilSimulation.FixedDelta;}
            }
            OilDrawing.Draw(Simulation);UpdateHud();
            if(!resultShown&&(Simulation.Won||Simulation.Failed))
            {
                resultShown=true;
                if(Simulation.Won)
                {
                    int stars=Simulation.SplitCount==0?3:Simulation.SplitCount==1?2:1;
                    string key="Oilpuz.mala.v1.stage."+stageIndex;PlayerPrefs.SetInt(key,Mathf.Max(stars,PlayerPrefs.GetInt(key)));PlayerPrefs.Save();
                    ShowModal("한 그릇, 완성했어요.",$"모든 기름을 한곳에 모았어요.\n갈라짐 {Simulation.SplitCount}회  ·  {new string('★',stars)}",stageIndex==stages.Length-1?"첫 그릇으로":"다음 그릇으로",()=>LoadStage((stageIndex+1)%stages.Length),"한 번 더 담기",()=>LoadStage(stageIndex));
                }
                else ShowModal("조금만 더 조심스럽게.",$"갈라짐 {Simulation.SplitCount}회 / 허용 {CurrentStage.splitLimit}회\n손끝의 장력을 낮춰 다시 도전해요.","다시 담기",()=>LoadStage(stageIndex),"이전 스테이지",()=>LoadStage(Mathf.Max(0,stageIndex-1)));
            }
            if(!string.IsNullOrEmpty(capturePath)&&frame==100)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capturePath)));
                CaptureFrame(capturePath);
                File.WriteAllText(capturePath+".json",$"{{\"stage\":{stageIndex+1},\"particles\":{Simulation.Particles.Count},\"groups\":{Simulation.GroupCount},\"splits\":{Simulation.SplitCount},\"grabbedParticle\":{Simulation.GrabbedParticle},\"tension\":{Simulation.Tension.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}");
            }
            if(!string.IsNullOrEmpty(capturePath)&&frame==145)Application.Quit();
        }
        PointerEventData DemoPointerEvent(Vector2 point)
        {
            return new PointerEventData(EventSystem.current){pointerId=-1,position=RectTransformUtility.WorldToScreenPoint(null,board.TransformPoint(point*400))};
        }
        void CaptureFrame(string path)
        {
            // A direct camera render is reliable even when Windows occludes the test player.
            var camera=Camera.main;var canvas=root.GetComponentInParent<Canvas>();
            var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);target.Create();
            var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;var oldTarget=camera.targetTexture;
            canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=2;camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();Layout();
            var request=new UniversalRenderPipeline.SingleCameraRequest{destination=target};
            RenderPipeline.SubmitRenderRequest(camera,request);
            var active=RenderTexture.active;RenderTexture.active=target;
            var pixels=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());
            RenderTexture.active=active;camera.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;target.Release();Destroy(target);Destroy(pixels);
        }
        void UpdateHud()
        {
            countText.text=Simulation.GroupCount.ToString();splitText.text=$"{Simulation.SplitCount} / {CurrentStage.splitLimit}";
            splitText.color=Simulation.SplitCount>CurrentStage.splitLimit?new Color(.7f,.22f,.09f):skin.ink;
            tensionFill.rectTransform.sizeDelta=new Vector2(Mathf.Max(5,Simulation.Tension*320),16);tensionFill.color=Simulation.Tension>.65f?new Color(.77f,.28f,.12f):skin.accent;
            tensionText.text=Simulation.Tension>.65f?"끊어질 것 같아요":Simulation.Dragging?"천천히, 좋아요":"고요해요";
            if(Simulation.MergeCount>previousMerges){Feedback("이어졌어요. 잡은 곳은 그대로, 천천히 옮겨요.");previousMerges=Simulation.MergeCount;}
            if(Simulation.SplitCount>previousSplits){Feedback("갈라졌어요. 장력을 낮춰 다시 모아보세요.");previousSplits=Simulation.SplitCount;}
            if(Simulation.BlockedByGate)Feedback("너무 커졌어요. 작은 방울부터 건너보세요.");
            if(UnityEngine.Time.unscaledTime>feedbackUntil)hintText.text=Simulation.GroupCount==1?(Simulation.InGoal?"손을 놓고 잠시 기다려요.":"하나가 됐어요. 초록 원 안으로 옮겨주세요."):CurrentStage.hint;
            touchRing.gameObject.SetActive(Simulation.Dragging);
            if(Simulation.Dragging)touchRing.rectTransform.anchoredPosition=Simulation.Pointer*400;
            goalRing.color=Simulation.InGoal?new Color(.82f,.98f,.59f):new Color(.65f,.84f,.53f,.85f);
            progressRing.progress=Simulation.SettleTime/1.1f;progressRing.SetVerticesDirty();
        }
        void Feedback(string message){hintText.text=message;feedbackUntil=UnityEngine.Time.unscaledTime+2;}
        void OnApplicationFocus(bool focus){if(!focus)input?.Cancel();}
        void OnApplicationPause(bool pause){if(pause&&CanPlay)TogglePause();}
        void OnDestroy(){OilDrawing?.Dispose();}

        static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform));var r=go.GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;return r;
        }
        static void Stretch(RectTransform r){r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;}
        Image Image(string name,Transform parent,Sprite sprite,Vector2 p,Vector2 size)
        {var r=Rect(name,parent,p,size);var i=r.gameObject.AddComponent<Image>();i.sprite=sprite;i.preserveAspect=true;i.raycastTarget=false;return i;}
        Text Label(string name,string text,Transform parent,Vector2 p,Vector2 size,int fontSize,Color color,FontStyle style=FontStyle.Normal)
        {var r=Rect(name,parent,p,size);var t=r.gameObject.AddComponent<Text>();t.font=skin.font;t.text=text;t.fontSize=fontSize;t.color=color;t.fontStyle=style;t.alignment=TextAnchor.MiddleCenter;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Overflow;return t;}
        UICard Card(string name,Transform parent,Vector2 p,Vector2 size,Color color,float radius=22)
        {var r=Rect(name,parent,p,size);var c=r.gameObject.AddComponent<UICard>();c.color=color;c.radius=radius;c.raycastTarget=false;return c;}
        Button Button(string name,string text,Transform parent,Vector2 p,Vector2 size,Action action,int fontSize)
        {
            var card=Card(name,parent,p,size,new Color(.95f,.9f,.79f,.94f),size.y*.5f);card.raycastTarget=true;var b=card.gameObject.AddComponent<Button>();b.targetGraphic=card;
            var colors=b.colors;colors.highlightedColor=new Color(1,1,.94f);colors.pressedColor=new Color(.86f,.84f,.76f);b.colors=colors;
            Label("Label",text,card.transform,Vector2.zero,size,fontSize,skin.ink);
            if(action!=null)b.onClick.AddListener(()=>action());return b;
        }
        UIRing Ring(string name,Transform parent,Vector2 p,Vector2 size,Color color,float thickness,int dashes)
        {var r=Rect(name,parent,p,size);var g=r.gameObject.AddComponent<UIRing>();g.color=color;g.thickness=thickness;g.dashCount=dashes;g.raycastTarget=false;return g;}
    }
}
