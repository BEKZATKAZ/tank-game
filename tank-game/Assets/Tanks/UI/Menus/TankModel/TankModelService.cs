using System.Collections.Generic;
using Tanks.Core.Players;
using Tanks.Client.Player;
using Tanks.UI.TankModel;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace Tanks.UI.Menus.TankModel
{
    public sealed class CurveEntry
    {
        public Transform TankModel;
        public Spline Spline;
        public float CurrentTime;
        public bool IsEnding;
    }

    public sealed class TankModelService : MonoBehaviour, ITankModelService
    {
        [SerializeField] private float _movementSpeed = 10;
        [SerializeField] private float _backgroundSizeUnits = 128;
        [SerializeField] private Transform _background;
        [SerializeField] private Transform _tankModelPrefab;
        [SerializeField] private SplineContainer _splineContainer;

        private readonly List<CurveEntry> _animations = new();

        public IReadOnlyList<TankModelEntry> Models => _models;
        public bool IsDrivingInPlace => _targetSpeed > 0;

        private readonly List<TankModelEntry> _models = new();
        private readonly Dictionary<int, EquippedCosmetics> _cosmeticPreviews = new();
        private Transform[] _tankModels;
        private float3 _splineOffset;

        private float _targetSpeed;
        private float _currentSpeed;

        private void ApplyCosmetics(int tankIndex)
        {
            if (_tankModels == null || tankIndex < 0 || tankIndex >= _tankModels.Length ||
                _tankModels[tankIndex] == false || tankIndex >= _models.Count) return;

            EquippedCosmetics cosmetics = _cosmeticPreviews.TryGetValue(tankIndex, out var preview)
                ? preview : _models[tankIndex].Equipped;

            _tankModels[tankIndex].GetComponent<CosmeticApplier>().Apply(cosmetics);
        }

        public void PrepareLocalPlayerTank()
        {
            _tankModels[0].position = new Vector3(0, -8.5f);
            _tankModels[0].gameObject.SetActive(true);
        }

        public void SetPlayerData(int tankIndex, PlayerDisplayData data)
        {
            _models[tankIndex].DisplayName = data.DisplayName ?? "";
            _models[tankIndex].Level = data.Level;
            _models[tankIndex].Equipped = data.Equipped;
            ApplyCosmetics(tankIndex);
        }

        public void PreviewCosmetics(int tankIndex, EquippedCosmetics cosmetics)
        {
            _cosmeticPreviews[tankIndex] = cosmetics;
            ApplyCosmetics(tankIndex);
        }

        public void ClearCosmeticPreview(int tankIndex)
        {
            _cosmeticPreviews.Remove(tankIndex);
            ApplyCosmetics(tankIndex);
        }

        public void SetReady(int tankIndex, bool ready)
        {
            _models[tankIndex].IsReady = ready;
        }

        public void Return(int tankIndex)
        {
            ClearCosmeticPreview(tankIndex);
            SetReady(tankIndex, false);
            Transform model = _tankModels[tankIndex];

            for (int i = 0; i < _animations.Count; i++)
            {
                if (_animations[i].TankModel != model) continue;
                _animations.RemoveAt(i);
                break;
            }

            if (tankIndex == 0)
            {
                model.SetPositionAndRotation(new Vector3(0, -8.5f), Quaternion.identity);
                return;
            }

            SetPlayerData(tankIndex, default);
            model.position = new Vector3(-999, -999);
            model.gameObject.SetActive(false);
        }

        public void DriveByCurve(int tankIndex, int curveIndex, bool instant)
        {
            Transform model = _tankModels[tankIndex];
            model.gameObject.SetActive(true);

            if (instant)
            {
                // Evaluate last point
                float3 position = _splineContainer.Splines[curveIndex].EvaluatePosition(1);
                position += _splineOffset;

                model.SetPositionAndRotation(position, Quaternion.Euler(0, 0, -90));
                return;
            }

            _animations.Add(new CurveEntry
            {
                TankModel = model,
                Spline = _splineContainer.Splines[curveIndex]
            });
        }

        public void StartDrivingInPlace()
        {
            _currentSpeed = 0;
            _targetSpeed = _movementSpeed;
        }

        public void StopDrivingInPlace()
        {
            _currentSpeed = 0;
            _targetSpeed = 0;
        }

        private void DriveInPlace(float deltaTime)
        {
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, _targetSpeed, deltaTime * 10);

            if (_currentSpeed <= 0.01f) return;
            Vector3 position = _background.position;

            position.x -= _currentSpeed * deltaTime;
            position.x %= _backgroundSizeUnits;

            _background.position = position;
        }

        private void Awake()
        {
            _tankModels = new Transform[_splineContainer.Splines.Count];

            for (int i = 0; i < _tankModels.Length; i++)
            {
                Vector3 position = new Vector3(-999, -999);

                _tankModels[i] = Instantiate(_tankModelPrefab, position, Quaternion.identity);
                _tankModels[i].GetComponentInChildren<TankModelEngineSound>().Initialize(this, i);
                _tankModels[i].gameObject.SetActive(false);

                _models.Add(new TankModelEntry(_tankModels[i]));
            }

            _splineOffset = _splineContainer.transform.position;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            DriveInPlace(deltaTime);

            for (int i = 0; i < _animations.Count; i++)
            {
                CurveEntry entry = _animations[i];
                entry.CurrentTime += deltaTime * 0.2f;

                if (entry.IsEnding)
                {
                    float newRotation = Mathf.MoveTowardsAngle(
                        entry.TankModel.eulerAngles.z, -90, deltaTime * 160);

                    entry.TankModel.rotation = Quaternion.Euler(0, 0, newRotation);
                    continue;
                }

                float t = math.sin(math.saturate(entry.CurrentTime) * math.PIHALF);

                entry.Spline.Evaluate(
                    t, out float3 position, out float3 tangent, out _);

                Vector2 direction = new(tangent.x, tangent.y);
                position += _splineOffset;

                float angle = direction.sqrMagnitude > 0.0001f
                    ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                    : entry.TankModel.eulerAngles.z;

                Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
                entry.TankModel.SetPositionAndRotation(position, rotation);

                if (entry.CurrentTime >= 1.1f) entry.IsEnding = true;
            }

            for (int i = 0; i < _animations.Count; i++)
            {
                if (_animations[i].CurrentTime < 2) continue;
                _animations.RemoveAt(i);
                i--;
            }
        }
    }
}
