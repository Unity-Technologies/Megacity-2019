using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.Gameplay
{
    /// <summary>
    /// Manages the player name tags.
    /// </summary>
    public class PlayerInfoController : MonoBehaviour
    {
        [HideInInspector]
        public string Name;
        [SerializeField]
        private PlayerInfoItemSettings m_Settings;
        [SerializeField]
        private VisualTreeAsset m_PlayerInfoItem;
        private VisualElement m_PlayerInfoContainer;
        private readonly Dictionary<Entity, PlayerInfoRef> NameTags = new();
        public static PlayerInfoController Instance;
        private Transform m_CameraTransform;
        private Camera m_Camera;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                m_Camera = Camera.main;
                if (m_Camera != null) 
                    m_CameraTransform = m_Camera.transform;
            }
            else
            {
                Destroy(this);
            }
        }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            m_PlayerInfoContainer = root.Q<VisualElement>("player-name-info-container");
        }

        private void OnDestroy()
        {
            Name = string.Empty;
        }

        public void CreateNameTag(string playerName, Entity player, float health)
        {
            if (NameTags.TryGetValue(player, out var nameTag))
            {
                var label = nameTag.Label;
                label.text = playerName;
            }
            else
            {
                var item = m_PlayerInfoItem.Instantiate();
                var playerInfo = new PlayerInfoRef(player, health, playerName, item, m_Settings);
                m_PlayerInfoContainer.Add(item);
                NameTags.Add(player, playerInfo);
            }
        }

        private void DestroyNameTag(Entity player)
        {
            m_PlayerInfoContainer.Remove(NameTags[player].Panel.parent);
            NameTags.Remove(player);
        }

        public void UpdateBadge(Entity player, bool shouldShow)
        {
            if (NameTags.TryGetValue(player, out var nameTag))
            {
                nameTag.UpdateBadge(shouldShow);
            }
        }

        public void UpdateNamePosition(Entity player, string playerName, float health, LocalToWorld localToWorld, CollisionWorld collisionWorld)
        {
            if (NameTags.ContainsKey(player))
            {
                NameTags[player].SetLife(health);
                var cameraPosition = m_CameraTransform.position;
                var distance = math.distancesq(localToWorld.Position, cameraPosition);
                var placerPosition = localToWorld.Position + GetOffsetByDistance(distance);
                var screenPosition = m_Camera.WorldToScreenPoint(placerPosition);
                var rootRay = cameraPosition + m_CameraTransform.forward * m_Settings.RayOffsetFromCamera;
                if (screenPosition.z < 0 || !NameTags[player].IsVisible(collisionWorld, rootRay, localToWorld.Position))
                {
                    NameTags[player].Hide();
                }
                else
                {
                    // Convert the screen position to panel position
                    screenPosition = RuntimePanelUtils.ScreenToPanel(m_PlayerInfoContainer.panel, new Vector2(screenPosition.x, Screen.height - screenPosition.y));
                    NameTags[player].UpdateLabel(playerName);
                    NameTags[player].UpdatePosition(screenPosition);
                    NameTags[player].UpdateScale(distance);
                    NameTags[player].Show();
                }
            }
        }

        private float3 GetOffsetByDistance(float distance)
        {
            var minOffset = m_Settings.MinOffset;
            var maxOffset = m_Settings.Offset;
            var minDistance = m_Settings.MinDistanceSq;
            var maxDistance = m_Settings.MaxDistanceSq;

            float3 offset;

            if (distance <= minDistance)
            {
                offset = Vector3.up * maxOffset;
            }
            else if (distance >= maxDistance)
            {
                offset = minOffset;
            }
            else
            {
                var t = (distance - minDistance) / (maxDistance - minDistance);
                offset = Vector3.up * math.lerp(maxOffset, minOffset, t);
            }

            return offset;
        }

        public void RefreshNameTags(EntityManager manager)
        {
            var list = new List<Entity>();
            foreach (var nameTag in NameTags.Keys)
            {
                if (!manager.Exists(nameTag))
                {
                    list.Add(nameTag);
                }
            }

            foreach (var entity in list)
            {
                DestroyNameTag(entity);
            }
        }
    }
}