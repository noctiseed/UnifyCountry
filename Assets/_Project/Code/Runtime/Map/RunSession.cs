using System.Collections.Generic;
using System.Linq;

namespace UnifyCountry.Map
{
    public static class RunSession
    {
        private const string StartNodeId = "NODE_YELLOW_TURBAN";

        private static readonly List<RunCampaignDefinition> campaigns = new List<RunCampaignDefinition>
        {
            new RunCampaignDefinition
            {
                CampaignId = "CAMPAIGN_YELLOW_TURBAN",
                Title = "黄巾之乱",
                Subtitle = "三关战役内容待填充",
                BattleLevelIds = { "LEVEL_001", "LEVEL_002", "LEVEL_003" }
            },
            new RunCampaignDefinition
            {
                CampaignId = "CAMPAIGN_HULAOGUAN",
                Title = "虎牢关之战",
                Subtitle = "三关战役内容待填充",
                BattleLevelIds = { "LEVEL_001", "LEVEL_002", "LEVEL_003" }
            },
            new RunCampaignDefinition
            {
                CampaignId = "CAMPAIGN_XUZHOU",
                Title = "徐州攻防战",
                Subtitle = "三关战役内容待填充",
                BattleLevelIds = { "LEVEL_001", "LEVEL_002", "LEVEL_003" }
            },
            new RunCampaignDefinition
            {
                CampaignId = "CAMPAIGN_LVBU",
                Title = "大战吕布",
                Subtitle = "最终 Boss 战内容待填充",
                BattleLevelIds = { "LEVEL_003" }
            }
        };

        private static readonly List<RunMapNodeDefinition> nodes = new List<RunMapNodeDefinition>
        {
            new RunMapNodeDefinition
            {
                NodeId = "NODE_YELLOW_TURBAN",
                NodeType = RunMapNodeType.Campaign,
                Title = "黄巾之乱",
                Subtitle = "普通战役 3 关",
                CampaignId = "CAMPAIGN_YELLOW_TURBAN",
                Position = new UnityEngine.Vector2(0.1f, 0.52f),
                NextNodeIds = { "NODE_BRANCH_REFUGEES", "NODE_BRANCH_MILITIA" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_BRANCH_REFUGEES",
                NodeType = RunMapNodeType.Branch,
                Title = "安抚流民",
                Subtitle = "分支占位",
                BranchEffectNote = "偏恢复、删牌、基础资源。当前仅记录路线选择。",
                Position = new UnityEngine.Vector2(0.26f, 0.68f),
                NextNodeIds = { "NODE_HULAOGUAN" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_BRANCH_MILITIA",
                NodeType = RunMapNodeType.Branch,
                Title = "收编义军",
                Subtitle = "分支占位",
                BranchEffectNote = "偏士兵牌、随机强化、小风险收益。当前仅记录路线选择。",
                Position = new UnityEngine.Vector2(0.26f, 0.36f),
                NextNodeIds = { "NODE_HULAOGUAN" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_HULAOGUAN",
                NodeType = RunMapNodeType.Campaign,
                Title = "虎牢关之战",
                Subtitle = "普通战役 3 关",
                CampaignId = "CAMPAIGN_HULAOGUAN",
                Position = new UnityEngine.Vector2(0.42f, 0.52f),
                NextNodeIds = { "NODE_BRANCH_SUPPLY", "NODE_BRANCH_RAID" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_BRANCH_SUPPLY",
                NodeType = RunMapNodeType.Branch,
                Title = "联军粮道",
                Subtitle = "分支占位",
                BranchEffectNote = "偏商店、金币、补给。当前仅记录路线选择。",
                Position = new UnityEngine.Vector2(0.56f, 0.68f),
                NextNodeIds = { "NODE_XUZHOU" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_BRANCH_RAID",
                NodeType = RunMapNodeType.Branch,
                Title = "夜袭敌营",
                Subtitle = "分支占位",
                BranchEffectNote = "偏遗物、情报、高风险奖励。当前仅记录路线选择，不触发战斗。",
                Position = new UnityEngine.Vector2(0.56f, 0.36f),
                NextNodeIds = { "NODE_XUZHOU" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_XUZHOU",
                NodeType = RunMapNodeType.Campaign,
                Title = "徐州攻防战",
                Subtitle = "普通战役 3 关",
                CampaignId = "CAMPAIGN_XUZHOU",
                Position = new UnityEngine.Vector2(0.7f, 0.52f),
                NextNodeIds = { "NODE_BRANCH_DEFEND", "NODE_BRANCH_CHASE" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_BRANCH_DEFEND",
                NodeType = RunMapNodeType.Branch,
                Title = "固守城池",
                Subtitle = "分支占位",
                BranchEffectNote = "偏最大生命、护甲、防御构筑。当前仅记录路线选择。",
                Position = new UnityEngine.Vector2(0.82f, 0.68f),
                NextNodeIds = { "NODE_LVBU" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_BRANCH_CHASE",
                NodeType = RunMapNodeType.Branch,
                Title = "追击吕布",
                Subtitle = "分支占位",
                BranchEffectNote = "偏攻击牌、灼烧、爆发构筑。当前仅记录路线选择。",
                Position = new UnityEngine.Vector2(0.82f, 0.36f),
                NextNodeIds = { "NODE_LVBU" }
            },
            new RunMapNodeDefinition
            {
                NodeId = "NODE_LVBU",
                NodeType = RunMapNodeType.Campaign,
                Title = "大战吕布",
                Subtitle = "最终 Boss 战 1 关",
                CampaignId = "CAMPAIGN_LVBU",
                Position = new UnityEngine.Vector2(0.94f, 0.52f)
            }
        };

        public static RunState Current { get; private set; }
        public static IReadOnlyList<RunMapNodeDefinition> Nodes => nodes;
        public static IReadOnlyList<RunCampaignDefinition> Campaigns => campaigns;
        public static bool HasActiveRun => Current != null;
        public static bool HasActiveBattle => Current != null && Current.IsBattleActive && !string.IsNullOrWhiteSpace(Current.ActiveCampaignId);

        public static void BeginNewRun()
        {
            Current = new RunState
            {
                CurrentNodeId = StartNodeId
            };
            Current.AvailableNodeIds.Add(StartNodeId);
        }

        public static RunMapNodeDefinition GetNode(string nodeId)
        {
            return nodes.FirstOrDefault(node => node.NodeId == nodeId);
        }

        public static RunCampaignDefinition GetCampaign(string campaignId)
        {
            return campaigns.FirstOrDefault(campaign => campaign.CampaignId == campaignId);
        }

        public static RunMapNodeState GetNodeState(string nodeId)
        {
            if (Current == null)
                return nodeId == StartNodeId ? RunMapNodeState.Available : RunMapNodeState.Locked;

            if (Current.CompletedNodeIds.Contains(nodeId))
                return RunMapNodeState.Completed;

            return Current.AvailableNodeIds.Contains(nodeId) ? RunMapNodeState.Available : RunMapNodeState.Locked;
        }

        public static bool TrySelectNode(string nodeId, out RunMapNodeDefinition node)
        {
            node = GetNode(nodeId);
            if (node == null || Current == null || GetNodeState(nodeId) != RunMapNodeState.Available)
                return false;

            if (node.NodeType == RunMapNodeType.Campaign)
            {
                Current.CurrentNodeId = node.NodeId;
                Current.ActiveCampaignId = node.CampaignId;
                Current.ActiveCampaignLevelIndex = 0;
                Current.IsBattleActive = true;
                return true;
            }

            CompleteNode(node);
            return true;
        }

        public static IReadOnlyList<string> GetActiveBattleLevelIds()
        {
            if (!HasActiveBattle)
                return new List<string>();

            var campaign = GetCampaign(Current.ActiveCampaignId);
            return campaign == null ? new List<string>() : campaign.BattleLevelIds;
        }

        public static int GetActiveCampaignLevelIndex()
        {
            return HasActiveBattle ? Current.ActiveCampaignLevelIndex : 0;
        }

        public static void SetActiveCampaignLevelIndex(int levelIndex)
        {
            if (HasActiveBattle)
                Current.ActiveCampaignLevelIndex = levelIndex;
        }

        public static void ReplaceDeckCounts(Dictionary<string, int> deckCounts)
        {
            if (Current == null || deckCounts == null)
                return;

            Current.DeckCounts.Clear();
            foreach (var entry in deckCounts)
                Current.DeckCounts[entry.Key] = entry.Value;
        }

        public static void CompleteActiveCampaign()
        {
            if (Current == null)
                return;

            var node = nodes.FirstOrDefault(candidate => candidate.CampaignId == Current.ActiveCampaignId);
            if (node != null)
                CompleteNode(node);

            Current.ActiveCampaignId = string.Empty;
            Current.ActiveCampaignLevelIndex = 0;
            Current.IsBattleActive = false;

            if (node != null && node.NodeId == "NODE_LVBU")
                Current.IsRunComplete = true;
        }

        private static void CompleteNode(RunMapNodeDefinition node)
        {
            Current.CompletedNodeIds.Add(node.NodeId);
            Current.AvailableNodeIds.Remove(node.NodeId);
            Current.CurrentNodeId = node.NodeId;

            if (node.NodeType == RunMapNodeType.Branch)
                RemoveSiblingBranchOptions(node);

            if (!Current.RouteHistory.Contains(node.Title))
                Current.RouteHistory.Add(node.Title);

            foreach (var nextNodeId in node.NextNodeIds)
            {
                if (!Current.CompletedNodeIds.Contains(nextNodeId))
                    Current.AvailableNodeIds.Add(nextNodeId);
            }
        }

        private static void RemoveSiblingBranchOptions(RunMapNodeDefinition selectedNode)
        {
            foreach (var node in nodes)
            {
                if (node.NodeType != RunMapNodeType.Branch || node.NodeId == selectedNode.NodeId)
                    continue;

                if (node.NextNodeIds.Any(nextNodeId => selectedNode.NextNodeIds.Contains(nextNodeId)))
                    Current.AvailableNodeIds.Remove(node.NodeId);
            }
        }
    }
}
