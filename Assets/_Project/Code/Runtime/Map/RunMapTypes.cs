using System.Collections.Generic;
using UnityEngine;

namespace UnifyCountry.Map
{
    public enum RunMapNodeType
    {
        Campaign,
        Branch
    }

    public enum RunMapNodeState
    {
        Locked,
        Available,
        Completed
    }

    public sealed class RunCampaignDefinition
    {
        public string CampaignId;
        public string Title;
        public string Subtitle;
        public readonly List<string> BattleLevelIds = new List<string>();
    }

    public sealed class RunMapNodeDefinition
    {
        public string NodeId;
        public RunMapNodeType NodeType;
        public string Title;
        public string Subtitle;
        public string CampaignId;
        public string BranchEffectNote;
        public Vector2 Position;
        public readonly List<string> NextNodeIds = new List<string>();
    }

    public sealed class RunState
    {
        public string CurrentNodeId;
        public string ActiveCampaignId;
        public int ActiveCampaignLevelIndex;
        public bool IsBattleActive;
        public bool IsRunComplete;
        public readonly HashSet<string> CompletedNodeIds = new HashSet<string>();
        public readonly HashSet<string> AvailableNodeIds = new HashSet<string>();
        public readonly Dictionary<string, int> DeckCounts = new Dictionary<string, int>();
        public readonly List<string> RouteHistory = new List<string>();
    }
}
