using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Tanks.Client.Input.Driving
{
    public sealed class DirectionalButtonProcessor : InputProcessor<float>
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Register()
        {
            if (InputSystem.TryGetProcessor("directionDeadzone") ==
                typeof(DirectionalButtonProcessor)) return;

            InputSystem.RegisterProcessor<DirectionalButtonProcessor>(
                "directionDeadzone");
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditor() => Register();
#endif

        public override float Process(float value, InputControl control)
        {
            if (control?.parent is not StickControl stick) return value;

            Vector2 direction = stick.ReadValue();

            return control.name switch
            {
                "up" => Mathf.Max(0, direction.y),
                "down" => Mathf.Max(0, -direction.y),
                "left" => Mathf.Max(0, -direction.x),
                "right" => Mathf.Max(0, direction.x),
                _ => value
            };
        }
    }
}
