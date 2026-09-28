using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Oilpuz.Editor
{
    public static class OilpuzUiValidation
    {
        [MenuItem("Oilpuz/Repair and validate UI input")]
        public static void RepairAndValidate()
        {
            var game = UnityEngine.Object.FindFirstObjectByType<OilGameController>();
            if (game == null) throw new Exception("Open MalaPuzzle before validating UI.");
            int repaired = 0;
            foreach (var graphic in game.GetComponentsInChildren<Graphic>(true))
            {
                if ((graphic is UICard || graphic is UIRing) && graphic.GetComponent<CanvasRenderer>() == null)
                {
                    Undo.AddComponent<CanvasRenderer>(graphic.gameObject);
                    graphic.SetAllDirty();
                    repaired++;
                }
                if (graphic.GetComponent<CanvasRenderer>() == null)
                    throw new Exception("Missing UI renderer: " + graphic.name);
            }
            if (!Application.isPlaying && repaired > 0)
            {
                EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
                EditorSceneManager.SaveScene(game.gameObject.scene);
            }
            if (!Application.isPlaying)
            {
                Debug.Log($"OILPUZ_UI_REPAIR_OK: repaired {repaired}. Enter Play mode and run again to validate event raycasts.");
                return;
            }
            // Validate the actual raycast path. Calling OnPointerDown directly bypasses it.
            Canvas.ForceUpdateCanvases();
            var surface = game.GetComponentInChildren<OilInputSurface>();
            AssertHit(surface.GetComponent<RectTransform>(), surface.gameObject);
            foreach (var button in game.GetComponentsInChildren<Button>())
                AssertHit(button.GetComponent<RectTransform>(), button.gameObject);
            // Future dynamically created cards/rings must also carry their renderer.
            var probe = new GameObject("UI requirement probe", typeof(RectTransform));
            try
            {
                probe.AddComponent<UICard>();
                if (probe.GetComponent<CanvasRenderer>() == null) throw new Exception("UICard renderer requirement missing.");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
            Debug.Log($"OILPUZ_UI_VALIDATION_OK: repaired {repaired}; oil surface and all active buttons pass EventSystem.RaycastAll.");
            game.StartCoroutine(ValidateMouseDrag(game, surface));
        }

        static IEnumerator ValidateMouseDrag(OilGameController game, OilInputSurface surface)
        {
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            var mouse = InputSystem.AddDevice<Mouse>("OilpuzValidationMouse");
            try
            {
                var rect = surface.GetComponent<RectTransform>();
                var sim = game.Simulation;
                Vector2 start = sim.Particles[0].position;
                Vector2 ScreenPoint(Vector2 p) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(p * (rect.rect.width * .5f)));
                InputSystem.QueueStateEvent(mouse, new MouseState { position = ScreenPoint(start) });
                yield return null; yield return null;
                InputSystem.QueueStateEvent(mouse, new MouseState { position = ScreenPoint(start) }.WithButton(MouseButton.Left));
                yield return null; yield return null;
                if (!sim.Dragging) throw new Exception("InputSystem mouse down did not grab oil.");
                int grabbed = sim.GrabbedParticle;
                var original = sim.Particles[grabbed].position;
                for (int i = 1; i <= 45; i++)
                {
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = ScreenPoint(start + new Vector2(.18f,.12f) * (i / 45f)) }.WithButton(MouseButton.Left));
                    yield return null;
                }
                if (!sim.Dragging || Vector2.Distance(original, sim.Particles[grabbed].position) < .1f)
                    throw new Exception("InputSystem mouse drag did not move the grabbed material.");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = ScreenPoint(start + new Vector2(.18f,.12f)) });
                yield return null; yield return null;
                if (sim.Dragging) throw new Exception("InputSystem mouse release did not release oil.");
                Debug.Log("OILPUZ_MOUSE_DRAG_OK: real InputSystem action dispatch, UI raycast, grab, movement and release passed.");
            }
            finally { InputSystem.RemoveDevice(mouse); }
        }

        static void AssertHit(RectTransform rect, GameObject expected)
        {
            var events = UnityEngine.Object.FindFirstObjectByType<EventSystem>();
            if (events == null) throw new Exception("Missing EventSystem.");
            var pointer = new PointerEventData(events)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center))
            };
            var hits = new List<RaycastResult>();
            events.RaycastAll(pointer, hits);
            if (hits.Count == 0 || hits[0].gameObject != expected)
                throw new Exception("UI raycast cannot reach " + expected.name);
        }
    }
}
