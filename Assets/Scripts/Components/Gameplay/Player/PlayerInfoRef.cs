using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MegaCity.Gameplay
{
    public struct PlayerInfoRef
    {
        public Entity Self;
        public string Name;
        public Label Label;
        public float3 CurrentScale;
        public VisualElement Panel;
        public VisualElement Badge;
        public VisualElement StatusIcon;
        public ProgressBar LifeBar;
        public PlayerInfoItemSettings Settings;

        public PlayerInfoRef(Entity player, float health, string playerName, VisualElement item, PlayerInfoItemSettings settings)
        {
            Label = item.Q<Label>("player-name");
            Badge = item.Q<VisualElement>("badge");
            LifeBar = item.Q<ProgressBar>("life-bar");
            Panel = item.Q<VisualElement>("player-info");
            StatusIcon = item.Q<VisualElement>("state-icon");
            Name = playerName;
            LifeBar.value = health;
            Settings = settings;
            Self = player;
            CurrentScale = new float3(1f,1f,1f);
            Label.text = playerName;
            SetChildrenUsageHint(LifeBar, UsageHints.DynamicTransform);
        }

        private void SetChildrenUsageHint(VisualElement element, UsageHints usageHints)
        {
            if (element.childCount < 1)
                return;

            foreach (var child in element.Children())
            {
                child.usageHints = usageHints;
                SetChildrenUsageHint(child, usageHints);
            }
        }

        public void UpdateBadge(bool shouldHightLightTheName)
        {
            if (!Label.ClassListContains("highlight"))
                Label.AddToClassList("highlight");

            Label.EnableInClassList("highlight", shouldHightLightTheName);
            Badge.style.display = shouldHightLightTheName? DisplayStyle.Flex : DisplayStyle.None;
        }

        public void UpdateLabel(string name)
        {
            if (!Name.Equals(name) || !Label.text.Equals(Name))
            {
                Name = name;
                Label.text = name;
            }
        }

        public void SetLife(float life)
        {
            if (LifeBar.value > 0 && life <= 0)
            {
                LifeBar.style.display = DisplayStyle.None;
                StatusIcon.style.display = DisplayStyle.Flex;
            }
            else if (LifeBar.value <= 0 && life > 0)
            {
                LifeBar.style.display = DisplayStyle.Flex;
                StatusIcon.style.display = DisplayStyle.None;
            }

            if (LifeBar.value >= Settings.MinLifeBar && life < Settings.MinLifeBar && !LifeBar.ClassListContains("magenta"))
            {
                if (Label.ClassListContains("magenta"))
                    Label.AddToClassList("magenta");

                Label.EnableInClassList("magenta", true);
                LifeBar.AddToClassList("magenta");
            }
            else if (LifeBar.value <= Settings.MinLifeBar && life > Settings.MinLifeBar && LifeBar.ClassListContains("magenta"))
            {
                Label.EnableInClassList("magenta", false);
                LifeBar.RemoveFromClassList("magenta");
            }

            LifeBar.value = life;
        }

        public void UpdatePosition(float3 pos2D)
        {
            pos2D.x -= (Panel.contentRect.size.x / 2);
            pos2D.y -= Panel.contentRect.size.y;
            Panel.transform.position = new Vector3(pos2D.x, pos2D.y, pos2D.z);
        }

        public void Hide ()
        {
            Panel.style.display = DisplayStyle.None;
        }

        public void Show()
        {
            Panel.style.display = DisplayStyle.Flex;
        }

        public bool IsVisible(CollisionWorld collisionWorld, float3 cameraPos, float3 playerPos)
        {
            var rayInput = new RaycastInput
            {
                Start = cameraPos,
                End = playerPos,
                Filter = CollisionFilter.Default
            };

            if (collisionWorld.CastRay(rayInput, out var closestHit))
            {
                var minDistance = Settings.MinDistanceBetweenCameraRayAndPlayer;
                return closestHit.Entity.Equals(Self) || math.distance(playerPos, closestHit.Position) < minDistance;
            }

            return true;
        }

        public void UpdateScale(float distance)
        {
            float scale = Settings.MinScale;
            if (distance <= Settings.MinDistanceSq)
            {
                scale = Settings.MaxScale;
            }
            else if (distance >= Settings.MaxDistanceSq)
            {
                scale = Settings.MinScale;
            }
            else
            {
                var t = (distance - Settings.MinDistanceSq) / (Settings.MaxDistanceSq - Settings.MinDistanceSq);
                scale = math.lerp(Settings.MaxScale, Settings.MinScale, t);
            }

            CurrentScale.xyz = scale;

            Panel.transform.scale = CurrentScale;
        }
    }
}
