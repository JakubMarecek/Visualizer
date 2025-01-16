using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace Visualizer
{
    public class NodeProps
    {
        Dictionary<string, string> DictVals { set; get; } = [];

        Dictionary<string, NodeProps> SubValues { set; get; } = [];

        public string this[string key]
        {
            get
            {
                return DictVals[key];
            }
            set
            {
                DictVals[key] = value;
            }
        }

        public NodeProps this[string key, string sub]
        {
            get
            {
                return null;
            }
            set
            {
                DictVals[key] = sub;

                SubValues[key] = value;
            }
        }

        public void AddRange(NodeProps props)
        {
            DictVals.AddRange(props.DictVals);
            SubValues.AddRange(props.SubValues);
        }

        public int Count()
        {
            return DictVals.Count;
        }

        public List<NodePropItem> GetData()
        {
            List<NodePropItem> outData = [];

            foreach (var kp in DictVals)
            {
                SubValues.TryGetValue(kp.Key, out var subs);
                outData.Add(new() { Name = kp.Key, Value = kp.Value, SubValues = subs });
            }

            return outData;
        }
    }

    public struct NodePropItem
    {
        public string Name { set; get; }

        public string Value { set; get; }

        public NodeProps SubValues { set; get; }
    }

    public class NodeProperties
    {
        public static NodeProps GetPropertiesForQuestNode(JToken node, JToken scnSceneResource = null)
        {
            NodeProps details = new();

            string nodeType = node.SelectToken("$type").Value<string>();

            details["Type"] = GetNameFromClass(nodeType);

            if (nodeType == "questInputNodeDefinition")
            {
                details["Input name"] = node.SelectToken("socketName.$value").Value<string>();
            }
            else if (nodeType == "questOutputNodeDefinition")
            {
                details["Type"] = node.SelectToken("type").Value<string>();
                details["Output name"] = node.SelectToken("socketName.$value").Value<string>();
            }
            else if (nodeType == "questPhaseNodeDefinition")
            {
                details["Phase resource"] = node.SelectToken("phaseResource.DepotPath.$value")?.Value<string>();
                details["Is graph"] = node.SelectToken("phaseGraph.HandleId") == null ? "False" : "True";
            }
            else if (nodeType == "questSceneNodeDefinition")
            {
                details["Filename"] = node.SelectToken("sceneFile.DepotPath.$value").Value<string>();
                details["Scene location"] = node.SelectToken("sceneLocation.nodeRef.$value").Value<string>();
            }
            else if (nodeType == "questPauseConditionNodeDefinition")
            {
                details.AddRange(GetPropertiesForConditions(node.SelectToken("condition.Data")));
            }
            else if (nodeType == "questConditionNodeDefinition")
            {
                details.AddRange(GetPropertiesForConditions(node.SelectToken("condition.Data")));
            }
            else if (nodeType == "questSwitchNodeDefinition")
            {
                int counter = 1;
                foreach (var cond in node.SelectToken("conditions"))
                {
                    details.AddRange(GetPropertiesForConditions(cond.SelectToken("condition.Data"), "Socket " + cond.SelectToken("socketId").Value<string>() + " "));
                    counter++;
                }
            }
            else if (nodeType == "questFactsDBManagerNodeDefinition")
            {
                var factDBManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = factDBManagerCasted.SelectToken("$type").Value<string>();

                if (nodeType2 == "questSetVar_NodeType")
                {
                    details["Fact Name"] = factDBManagerCasted.SelectToken("factName").Value<string>();
                    details["Set Exact Value"] = factDBManagerCasted.SelectToken("setExactValue").Value<string>() == "1" ? "True" : "False";
                    details["Value"] = factDBManagerCasted.SelectToken("value").Value<string>();
                }
            }
            else if (nodeType == "questJournalNodeDefinition")
            {
                var journalNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = journalNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questJournalQuestEntry_NodeType")
                {
                    details.AddRange(ParseJournalPath(journalNodeCasted.SelectToken("path.Data")));

                    details["Send Notification"] = journalNodeCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";
                    details["Track Quest"] = journalNodeCasted.SelectToken("trackQuest").Value<string>() == "1" ? "True" : "False";
                    details["Version"] = journalNodeCasted.SelectToken("version").Value<string>();
                }
                if (nodeType2 == "questJournalEntry_NodeType")
                {
                    details.AddRange(ParseJournalPath(journalNodeCasted.SelectToken("path.Data")));

                    details["Send Notification"] = journalNodeCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questJournalBulkUpdate_NodeType")
                {
                    details["New Entry State"] = journalNodeCasted.SelectToken("newEntryState.$value").Value<string>();
                    details.AddRange(ParseJournalPath(journalNodeCasted.SelectToken("path.Data")));
                    details["Propagate Change"] = journalNodeCasted.SelectToken("propagateChange").Value<string>() == "1" ? "True" : "False";
                    details["Required Entry State"] = journalNodeCasted.SelectToken("requiredEntryState.$value").Value<string>();
                    details["Required Entry Type"] = journalNodeCasted.SelectToken("requiredEntryType.$value").Value<string>();
                    details["Send Notification"] = journalNodeCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questUseWorkspotNodeDefinition")
            {
                var useWorkspotNodeCasted = node.SelectToken("paramsV1.Data");

                string nodeType2 = useWorkspotNodeCasted.SelectToken("$type").Value<string>();

                details["Type"] = GetNameFromClass(nodeType2);
                details["Entity Reference"] = ParseGameEntityReference(node.SelectToken("entityReference"));

                if (nodeType2 == "scnUseSceneWorkspotParamsV1")
                {
                    details["Entry Id"] = useWorkspotNodeCasted.SelectToken("entryId.id").Value<string>();
                    details["Exit Entry Id"] = useWorkspotNodeCasted.SelectToken("exitEntryId.id").Value<string>();
                    details["Workspot Instance Id"] = useWorkspotNodeCasted.SelectToken("workspotInstanceId.id").Value<string>();
                    details["Workspot Name"] = GetWorkspotPath(useWorkspotNodeCasted.SelectToken("workspotInstanceId.id").Value<string>(), scnSceneResource);
                }
                else if (nodeType2 == "questUseWorkspotParamsV1")
                {
                    details["Entry Id"] = useWorkspotNodeCasted.SelectToken("entryId.id").Value<string>();
                    details["Exit Entry Id"] = useWorkspotNodeCasted.SelectToken("exitEntryId.id").Value<string>();
                    details["Workspot Node"] = useWorkspotNodeCasted.SelectToken("workspotNode.$value").Value<string>();
                }
            }
            else if (nodeType == "questSceneManagerNodeDefinition")
            {
                var sceneManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = sceneManagerNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questSetTier_NodeType")
                {
                    details["Force Empty Hands"] = sceneManagerNodeCasted.SelectToken("forceEmptyHands").Value<string>() == "1" ? "True" : "False";
                    details["Tier"] = sceneManagerNodeCasted.SelectToken("tier").Value<string>();
                }
                if (nodeType2 == "questCameraClippingPlane_NodeType")
                {
                    details["Preset"] = sceneManagerNodeCasted.SelectToken("preset").Value<string>();
                }
                if (nodeType2 == "questToggleEventExecutionTag_NodeType")
                {
                    details["Event Execution Tag"] = sceneManagerNodeCasted.SelectToken("eventExecutionTag.$value").Value<string>();
                    details["Mute"] = sceneManagerNodeCasted.SelectToken("mute").Value<string>() == "1" ? "True" : "False";
                    details["Scene File"] = sceneManagerNodeCasted.SelectToken("sceneFile.DepotPath.$value").Value<string>();
                }
            }
            else if (nodeType == "questTimeManagerNodeDefinition")
            {
                var timeManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = timeManagerNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questPauseTime_NodeType")
                {
                    details["Pause"] = timeManagerNodeCasted.SelectToken("pause").Value<string>() == "1" ? "True" : "False";
                    details["Source"] = timeManagerNodeCasted.SelectToken("source.$value").Value<string>();
                }
                if (nodeType2 == "questSetTime_NodeType")
                {
                    details["Hours"] = timeManagerNodeCasted.SelectToken("hours").Value<string>();
                    details["Minutes"] = timeManagerNodeCasted.SelectToken("minutes").Value<string>();
                    details["Seconds"] = timeManagerNodeCasted.SelectToken("seconds").Value<string>();
                    details["Source"] = timeManagerNodeCasted.SelectToken("source.$value").Value<string>();
                }
            }
            else if (nodeType == "questAudioNodeDefinition")
            {
                var audioNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = audioNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questAudioMixNodeType")
                {
                    details["Mix Signpost"] = audioNodeCasted.SelectToken("mixSignpost.$value").Value<string>();
                }
                if (nodeType2 == "questAudioEventNodeType")
                {
                    details["Ambient Unique Name"] = audioNodeCasted.SelectToken("ambientUniqueName.$value").Value<string>();

                    var dynamicParams = "";
                    foreach (var p in audioNodeCasted.SelectToken("dynamicParams"))
                    {
                        dynamicParams += (dynamicParams != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                    }
                    details["Dynamic Params"] = dynamicParams;

                    details["Emitter"] = audioNodeCasted.SelectToken("emitter.$value").Value<string>();
                    details["Event"] = audioNodeCasted.SelectToken("event.event.$value").Value<string>();

                    var events = "";
                    foreach (var p in audioNodeCasted.SelectToken("events"))
                    {
                        events += (events != "" ? ", " : "") + p.SelectToken("event.$value").Value<string>();
                    }
                    details["Events"] = events;

                    details["Is Music"] = audioNodeCasted.SelectToken("isMusic").Value<string>() == "1" ? "True" : "False";
                    details["Is Player"] = audioNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";

                    var musicEvents = "";
                    foreach (var p in audioNodeCasted.SelectToken("musicEvents"))
                    {
                        musicEvents += (musicEvents != "" ? ", " : "") + p.SelectToken("event.$value").Value<string>();
                    }
                    details["Music Events"] = musicEvents;

                    details["Object Ref"] = ParseGameEntityReference(audioNodeCasted.SelectToken("objectRef"));

                    var paramsStr = "";
                    foreach (var p in audioNodeCasted.SelectToken("params"))
                    {
                        paramsStr += (paramsStr != "" ? ", " : "") + p.SelectToken("name.$value").Value<string>();
                    }
                    details["Params"] = paramsStr;

                    var switches = "";
                    foreach (var p in audioNodeCasted.SelectToken("switches"))
                    {
                        switches += (switches != "" ? ", " : "") + p.SelectToken("name.$value").Value<string>();
                    }
                    details["Switches"] = switches;
                }
                if (nodeType2 == "questAudioSwitchNodeType")
                {
                    details["Is Music"] = audioNodeCasted.SelectToken("isMusic").Value<string>() == "1" ? "True" : "False";
                    details["Is Player"] = audioNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    details["Object Ref"] = ParseGameEntityReference(audioNodeCasted.SelectToken("objectRef"));
                    details["Switch Name"] = audioNodeCasted.SelectToken("switch.name.$value").Value<string>();
                    details["Switch Value"] = audioNodeCasted.SelectToken("switch.value.$value").Value<string>();
                }
            }
            else if (nodeType == "questEventManagerNodeDefinition")
            {
                string nodeType2 = node.SelectToken("event.Data.$type").Value<string>();

                details["Component Name"] = node.SelectToken("componentName.$value").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "DisableBraindanceActions")
                {
                    subProps.AddRange(ParseBDMask(node.SelectToken("event.Data.actionMask")));
                }
                if (nodeType2 == "EnableBraindanceActions")
                {
                    subProps.AddRange(ParseBDMask(node.SelectToken("event.Data.actionMask")));
                }
                if (nodeType2 == "gameActionEvent")
                {
                    subProps["Event Action"] = node.SelectToken("event.Data.eventAction.$value").Value<string>();
                    subProps["Internal Event"] = node.SelectToken("event.Data.internalEvent.Data.$type")?.Value<string>();
                    subProps["Name"] = node.SelectToken("event.Data.name.$value").Value<string>();
                    subProps["Time To Live"] = node.SelectToken("event.Data.timeToLive").Value<string>();
                }

                details["Event", nodeType2] = subProps;
                details["Is Object Player"] = node.SelectToken("isObjectPlayer").Value<string>() == "1" ? "True" : "False";
                details["Manager Name"] = node.SelectToken("managerName").Value<string>();
                details["Object Ref"] = ParseGameEntityReference(node.SelectToken("objectRef"));
            }
            else if (nodeType == "questEnvironmentManagerNodeDefinition")
            {
                var envManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = envManagerNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questPlayEnv_SetWeather")
                {
                    details["Blend Time"] = envManagerNodeCasted.SelectToken("blendTime").Value<string>();
                    details["Priority"] = envManagerNodeCasted.SelectToken("priority").Value<string>();
                    details["Reset"] = envManagerNodeCasted.SelectToken("reset").Value<string>() == "1" ? "True" : "False";
                    details["Source"] = envManagerNodeCasted.SelectToken("source.$value").Value<string>();
                    details["Weather ID"] = envManagerNodeCasted.SelectToken("weatherID.$value").Value<string>();
                }
            }
            else if (nodeType == "questRenderFxManagerNodeDefinition")
            {
                var renderFxManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = renderFxManagerNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questSetFadeInOut_NodeType")
                {
                    details["Duration"] = renderFxManagerNodeCasted.SelectToken("duration").Value<string>();
                    details["Fade Color"] = ParseColor(renderFxManagerNodeCasted.SelectToken("fadeColor"));
                    details["Fade In"] = renderFxManagerNodeCasted.SelectToken("fadeIn").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questUIManagerNodeDefinition")
            {
                var uiManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = uiManagerNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questSetHUDEntryForcedVisibility_NodeType")
                {
                    var p = uiManagerNodeCasted.SelectToken("hudEntryName");
                    for (int i = 0; i < p.Count(); i++)
                    {
                        details["Hud Entry Name #" + i] = p[i].SelectToken("$value").Value<string>();
                    }

                    details["Hud Visibility Preset"] = uiManagerNodeCasted.SelectToken("hudVisibilityPreset.$value").Value<string>();
                    details["Skip Animation"] = uiManagerNodeCasted.SelectToken("skipAnimation").Value<string>() == "1" ? "True" : "False";
                    details["Use Preset"] = uiManagerNodeCasted.SelectToken("usePreset").Value<string>() == "1" ? "True" : "False";
                    details["Visibility"] = uiManagerNodeCasted.SelectToken("visibility").Value<string>();
                }
                if (nodeType2 == "questSwitchNameplate_NodeType")
                {
                    details["Alternative Name"] = uiManagerNodeCasted.SelectToken("alternativeName").Value<string>() == "1" ? "True" : "False";
                    details["Enable"] = uiManagerNodeCasted.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                    details["Is Player"] = uiManagerNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    details["Puppet Ref"] = ParseGameEntityReference(uiManagerNodeCasted.SelectToken("puppetRef"));
                }
                if (nodeType2 == "questWarningMessage_NodeType")
                {
                    details["Duration"] = uiManagerNodeCasted.SelectToken("duration").Value<string>();
                    details["Instant"] = uiManagerNodeCasted.SelectToken("instant").Value<string>() == "1" ? "True" : "False";
                    details["Localized Message"] = uiManagerNodeCasted.SelectToken("localizedMessage.value").Value<string>();
                    details["Message"] = uiManagerNodeCasted.SelectToken("message").Value<string>();
                    details["Show"] = uiManagerNodeCasted.SelectToken("show").Value<string>() == "1" ? "True" : "False";
                    details["Message Type"] = uiManagerNodeCasted.SelectToken("type").Value<string>();
                }
                if (nodeType2 == "questProgressBar_NodeType")
                {
                    details["Bottom Text"] = uiManagerNodeCasted.SelectToken("bottomText.value").Value<string>();
                    details["Duration"] = uiManagerNodeCasted.SelectToken("duration").Value<string>();
                    details["Show"] = uiManagerNodeCasted.SelectToken("show").Value<string>() == "1" ? "True" : "False";
                    details["Text"] = uiManagerNodeCasted.SelectToken("text.value").Value<string>();
                    details["Progress Bar Type"] = uiManagerNodeCasted.SelectToken("type").Value<string>();
                }
                if (nodeType2 == "questTutorial_NodeType")
                {
                    var tutorialCasted = uiManagerNodeCasted.SelectToken("subtype.Data");

                    string nodeType3 = tutorialCasted.SelectToken("$type").Value<string>();

                    details["Sub Manager"] = GetNameFromClass(nodeType3);

                    if (nodeType3 == "questShowPopup_NodeSubType")
                    {
                        details["Close At Input"] = tutorialCasted.SelectToken("closeAtInput").Value<string>() == "1" ? "True" : "False";
                        details["Close Current Popup"] = tutorialCasted.SelectToken("closeCurrentPopup").Value<string>() == "1" ? "True" : "False";
                        details["Hide In Menu"] = tutorialCasted.SelectToken("hideInMenu").Value<string>() == "1" ? "True" : "False";
                        details["Ignore Disabled Tutorials"] = tutorialCasted.SelectToken("ignoreDisabledTutorials").Value<string>() == "1" ? "True" : "False";
                        details["Lock Player Movement"] = tutorialCasted.SelectToken("lockPlayerMovement").Value<string>() == "1" ? "True" : "False";
                        details["Margin"] = ParseMargin(tutorialCasted.SelectToken("margin"));
                        details["Open"] = tutorialCasted.SelectToken("open").Value<string>() == "1" ? "True" : "False";
                        details.AddRange(ParseJournalPath(tutorialCasted.SelectToken("path")));
                        details["Pause Game"] = tutorialCasted.SelectToken("pauseGame").Value<string>() == "1" ? "True" : "False";
                        details["Position"] = tutorialCasted.SelectToken("position").Value<string>();
                        details["Screen Mode"] = tutorialCasted.SelectToken("screenMode").Value<string>();
                        details["Video"] = tutorialCasted.SelectToken("video.DepotPath.$value").Value<string>();
                        details["Video Type"] = tutorialCasted.SelectToken("videoType").Value<string>();
                    }
                    if (nodeType3 == "questShowBracket_NodeSubType")
                    {
                        details["Anchor"] = tutorialCasted.SelectToken("anchor").Value<string>();
                        details["Bracket ID"] = tutorialCasted.SelectToken("bracketID").Value<string>();
                        details["Bracket Type"] = tutorialCasted.SelectToken("bracketType").Value<string>();
                        details["Ignore Disabled Tutorials"] = tutorialCasted.SelectToken("ignoreDisabledTutorials").Value<string>() == "1" ? "True" : "False";
                        details["Offset"] = tutorialCasted.SelectToken("offset").Value<string>();
                        details["Size"] = tutorialCasted.SelectToken("size").Value<string>();
                        details["Visible"] = tutorialCasted.SelectToken("visible").Value<string>() == "1" ? "True" : "False";
                        details["Visible On UI Layer"] = tutorialCasted.SelectToken("visibleOnUILayer").Value<string>();
                    }
                    if (nodeType3 == "questShowHighlight_NodeSubType")
                    {
                        details["Enable"] = tutorialCasted.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                        details["Entity Reference"] = ParseGameEntityReference(tutorialCasted.SelectToken("entityReference"));
                    }
                    if (nodeType3 == "questShowOverlay_NodeSubType")
                    {
                        details["Hide On Input"] = tutorialCasted.SelectToken("hideOnInput").Value<string>() == "1" ? "True" : "False";
                        details["Library Item Name"] = tutorialCasted.SelectToken("libraryItemName").Value<string>();
                        details["Lock Player Movement"] = tutorialCasted.SelectToken("lockPlayerMovement").Value<string>() == "1" ? "True" : "False";
                        details["Overlay Library"] = tutorialCasted.SelectToken("overlayLibrary.DepotPath.$value").Value<string>();
                        details["Pause Game"] = tutorialCasted.SelectToken("pauseGame").Value<string>() == "1" ? "True" : "False";
                        details["Visible"] = tutorialCasted.SelectToken("visible").Value<string>() == "1" ? "True" : "False";
                    }
                }
            }
            else if (nodeType == "questTeleportPuppetNodeDefinition")
            {
                details["Entity Reference"] = GetNameFromUniversalRef(node.SelectToken("entityReference.Data"));
                //details["Local Player"] = teleportNodeCasted?.EntityReference?.Chunk?.RefLocalPlayer == true ? "True" : "False";
                details["Look At Action"] = node.SelectToken("lookAtAction").Value<string>();

                details["Destination Offset"] = ParseVector(node.SelectToken("params.Data.destinationOffset"));
                details["Destination Entity Reference"] = GetNameFromUniversalRef(node.SelectToken("params.Data.destinationRef.Data"));
                details["Heal At Teleport"] = node.SelectToken("params.Data.healAtTeleport").Value<string>() == "1" ? "True" : "False";

                details["Player Look At Offset"] = ParseVector(node?.SelectToken("playerLookAt.Data.offset"));
                details["Player Look At Target"] = ParseGameEntityReference(node?.SelectToken("playerLookAt.Data.lookAtTarget"));
            }
            else if (nodeType == "questWorldDataManagerNodeDefinition")
            {
                var worldDataManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = worldDataManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questShowWorldNode_NodeType")
                {
                    details["Component Name"] = worldDataManagerCasted.SelectToken("componentName.$value").Value<string>();
                    details["Is Player"] = worldDataManagerCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    details["Object Ref"] = worldDataManagerCasted.SelectToken("objectRef.$value").Value<string>();
                    details["Show"] = worldDataManagerCasted.SelectToken("show").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questTogglePrefabVariant_NodeType")
                {
                    var paramsArr = worldDataManagerCasted.SelectToken("params");
                    //details["Params Count"] = paramsArr?.Count.ToString()!;

                    int counter = 1;
                    foreach (var re in paramsArr)
                    {
                        details["#" + counter + " Prefab Node Ref"] = re.SelectToken("prefabNodeRef.$value").Value<string>();

                        var variantStates = re.SelectToken("variantStates");
                        //details["#" + counter + " Variant States Count"] = variantStates?.Count.ToString()!;

                        int counter2 = 1;
                        foreach (var vs in variantStates)
                        {
                            details["#" + counter + " Variant State #" + counter2 + " Name"] = vs.SelectToken("name.$value").Value<string>();
                            details["#" + counter + " Variant State #" + counter2 + " Show"] = vs.SelectToken("show").Value<string>() == "1" ? "True" : "False";

                            counter2++;
                        }

                        counter++;
                    }
                }
                if (nodeType2 == "questPrefetchStreaming_NodeTypeV2")
                {
                    details["Force Enable"] = worldDataManagerCasted.SelectToken("forceEnable").Value<string>() == "1" ? "True" : "False";
                    details["Max Distance"] = worldDataManagerCasted.SelectToken("maxDistance").Value<string>();
                    details["Prefetch Position Ref"] = worldDataManagerCasted.SelectToken("prefetchPositionRef.$value").Value<string>();
                    details["Use Streaming Occlusion"] = worldDataManagerCasted.SelectToken("useStreamingOcclusion").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questSpawnManagerNodeDefinition")
            {
                var actions = node.SelectToken("actions");
                //details["Actions"] = actions.Count.ToString();

                int counter = 1;
                foreach (var action in actions)
                {
                    var actionCasted = action.SelectToken("type.Data");

                    if (actionCasted == null) continue;

                    string nodeType2 = actionCasted.SelectToken("$type").Value<string>();

                    if (nodeType2 == "questScene_NodeType")
                    {
                        details["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        details["#" + counter + " Entity Reference"] = ParseGameEntityReference(actionCasted.SelectToken("entityReference"));
                    }
                    if (nodeType2 == "questCommunityTemplate_NodeType")
                    {
                        details["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        details["#" + counter + " Community Entry Name"] = actionCasted.SelectToken("communityEntryName.$value").Value<string>();
                        details["#" + counter + " Community Entry Phase Name"] = actionCasted.SelectToken("communityEntryPhaseName.$value").Value<string>();
                        details["#" + counter + " Spawner Reference"] = actionCasted.SelectToken("spawnerReference.$value").Value<string>();
                    }
                    if (nodeType2 == "questSpawnSet_NodeType")
                    {
                        details["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        details["#" + counter + " Entry Name"] = actionCasted.SelectToken("entryName.$value").Value<string>();
                        details["#" + counter + " Phase Name"] = actionCasted.SelectToken("phaseName.$value").Value<string>();
                        details["#" + counter + " Reference"] = actionCasted.SelectToken("reference.$value").Value<string>();
                    }
                    if (nodeType2 == "questSpawner_NodeType")
                    {
                        details["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        details["#" + counter + " Spawner Reference"] = actionCasted.SelectToken("spawnerReference.$value").Value<string>();
                    }

                    counter++;
                }
            }
            else if (nodeType == "questGameManagerNodeDefinition")
            {
                var gameManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = gameManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questGameplayRestrictions_NodeType")
                {
                    details["Action"] = gameManagerCasted.SelectToken("action").Value<string>();

                    var restr = gameManagerCasted.SelectToken("restrictionIDs");
                    //details["Restrictions"] = restr?.Count.ToString()!;

                    int counter = 1;
                    foreach (var re in restr)
                    {
                        details["#" + counter] = re.SelectToken("$value").Value<string>();

                        counter++;
                    }
                }
                if (nodeType2 == "questRumble_NodeType")
                {
                    details["Is Player"] = gameManagerCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    details["Object Ref"] = ParseGameEntityReference(gameManagerCasted.SelectToken("objectRef"));
                    details["Rumble Event"] = gameManagerCasted.SelectToken("rumbleEvent.$value").Value<string>();
                }
                if (nodeType2 == "questContentTokenManager_NodeType")
                {
                    var contentTokenManagerNodeCasted = gameManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = contentTokenManagerNodeCasted.SelectToken("$type").Value<string>();

                    details["Sub Manager"] = GetNameFromClass(nodeType3);

                    if (nodeType3 == "questBlockTokenActivation_NodeSubType")
                    {
                        details["Action"] = contentTokenManagerNodeCasted.SelectToken("action").Value<string>();
                        details["Reset Token Spawn Timer"] = contentTokenManagerNodeCasted.SelectToken("resetTokenSpawnTimer").Value<string>() == "1" ? "True" : "False";
                        details["Source"] = contentTokenManagerNodeCasted.SelectToken("source.$value").Value<string>();
                    }
                }
            }
            else if (nodeType == "questVoicesetManagerNodeDefinition")
            {
                var voicesetManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = voicesetManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questPlayVoiceset_NodeType")
                {
                    var paramsArr = voicesetManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        details["#" + counter + " Is Player"] = param.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Override Visual Style"] = param.SelectToken("overrideVisualStyle").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Override Voiceover Expression"] = param.SelectToken("overrideVoiceoverExpression").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Overriding Visual Style"] = param.SelectToken("overridingVisualStyle").Value<string>();
                        details["#" + counter + " Overriding Voiceover Context"] = param.SelectToken("overridingVoiceoverContext").Value<string>();
                        details["#" + counter + " Overriding Voiceover Expression"] = param.SelectToken("overridingVoiceoverExpression").Value<string>();
                        details["#" + counter + " Play Only Grunt"] = param.SelectToken("playOnlyGrunt").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Puppet Ref"] = ParseGameEntityReference(param.SelectToken("puppetRef"));
                        details["#" + counter + " Use Voiceset System"] = param.SelectToken("useVoicesetSystem").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Voiceset Name"] = param.SelectToken("voicesetName.$value").Value<string>();

                        counter++;
                    }
                }
            }
            else if (nodeType == "questInteractiveObjectManagerNodeDefinition")
            {
                var interactiveObjectManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = interactiveObjectManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questDeviceManager_NodeType")
                {
                    var paramsArr = interactiveObjectManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        var paramCasted = param.SelectToken("Data");

                        var actionProperties = "";
                        foreach (var p in paramCasted.SelectToken("actionProperties"))
                        {
                            actionProperties += (actionProperties != "" ? ", " : "") + p.SelectToken("name.$value").Value<string>();
                        }
                        details["#" + counter + " Action Properties"] = actionProperties;

                        details["#" + counter + " Device Action"] = paramCasted.SelectToken("deviceAction.$value").Value<string>();
                        details["#" + counter + " Device Controller Class"] = paramCasted.SelectToken("deviceControllerClass.$value").Value<string>();
                        details["#" + counter + " Entity Ref"] = ParseGameEntityReference(paramCasted.SelectToken("entityRef"));
                        details["#" + counter + " Object Ref"] = paramCasted.SelectToken("objectRef.$value").Value<string>();
                        details["#" + counter + " Slot Name"] = paramCasted.SelectToken("slotName.$value").Value<string>();

                        counter++;
                    }
                }
            }
            else if (nodeType == "questCharacterManagerNodeDefinition")
            {
                var characterManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = characterManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questCharacterManagerParameters_NodeType")
                {
                    var charParamsNodeCasted = characterManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = charParamsNodeCasted.SelectToken("$type").Value<string>();

                    details["Sub Manager"] = GetNameFromClass(nodeType3);

                    if (nodeType3 == "questCharacterManagerParameters_SetStatusEffect")
                    {
                        details["Is Player"] = charParamsNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        details["Is Player Status Effect Source"] = charParamsNodeCasted.SelectToken("isPlayerStatusEffectSource").Value<string>() == "1" ? "True" : "False";
                        details["Puppet Ref"] = ParseGameEntityReference(charParamsNodeCasted.SelectToken("puppetRef"));
                        //setStatusEffectNodeCasted?.RecordSelector
                        details["Set"] = charParamsNodeCasted.SelectToken("set").Value<string>() == "1" ? "True" : "False";
                        details["Status Effect ID"] = charParamsNodeCasted.SelectToken("statusEffectID.$value").Value<string>();
                        details["Status Effect Source Object"] = ParseGameEntityReference(charParamsNodeCasted.SelectToken("statusEffectSourceObject"));
                    }
                }
                if (nodeType2 == "questCharacterManagerVisuals_NodeType")
                {
                    var charVisualsNodeCasted = characterManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = charVisualsNodeCasted.SelectToken("$type").Value<string>();

                    details["Sub Manager"] = GetNameFromClass(nodeType3);

                    if (nodeType3 == "questCharacterManagerVisuals_GenitalsManager")
                    {
                        details["Body Group Name"] = charVisualsNodeCasted.SelectToken("bodyGroupName.$value").Value<string>();
                        details["Enable"] = charVisualsNodeCasted.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                        details["Is Player"] = charVisualsNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        details["Puppet Ref"] = ParseGameEntityReference(charVisualsNodeCasted.SelectToken("puppetRef"));
                    }
                    if (nodeType3 == "questCharacterManagerVisuals_ChangeEntityAppearance")
                    {
                        var appearancesArr = charVisualsNodeCasted.SelectToken("appearanceEntries");

                        int counter = 1;
                        foreach (var appearance in appearancesArr)
                        {
                            details["#" + counter + " Appearance Name"] = appearance.SelectToken("appearanceName.$value").Value<string>();
                            details["#" + counter + " Is Player"] = appearance.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                            details["#" + counter + " Puppet Ref"] = ParseGameEntityReference(appearance.SelectToken("puppetRef"));

                            counter++;
                        }
                    }
                }
                if (nodeType2 == "questCharacterManagerCombat_NodeType")
                {
                    var charCombatNodeCasted = characterManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = charCombatNodeCasted.SelectToken("$type").Value<string>();

                    details["Sub Manager"] = GetNameFromClass(nodeType3);

                    if (nodeType3 == "questCharacterManagerCombat_EquipWeapon")
                    {
                        details["Equip"] = charCombatNodeCasted.SelectToken("equip").Value<string>() == "1" ? "True" : "False";
                        details["Equip Last Weapon"] = charCombatNodeCasted.SelectToken("equipLastWeapon").Value<string>() == "1" ? "True" : "False";
                        details["Force First Equip"] = charCombatNodeCasted.SelectToken("forceFirstEquip").Value<string>() == "1" ? "True" : "False";
                        details["Ignore State Machine"] = charCombatNodeCasted.SelectToken("ignoreStateMachine").Value<string>() == "1" ? "True" : "False";
                        details["Instant"] = charCombatNodeCasted.SelectToken("instant").Value<string>() == "1" ? "True" : "False";
                        details["Slot ID"] = charCombatNodeCasted.SelectToken("slotID.$value").Value<string>();
                        details["Weapon ID"] = charCombatNodeCasted.SelectToken("weaponID.$value").Value<string>();
                    }
                }
            }
            else if (nodeType == "questItemManagerNodeDefinition")
            {
                var itemManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = itemManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questAddRemoveItem_NodeType")
                {
                    var paramsArr = itemManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        var paramCasted = param.SelectToken("Data");

                        details["#" + counter + " Entity Ref"] = GetNameFromUniversalRef(paramCasted.SelectToken("entityRef"));
                        details["#" + counter + " Flag Item Added Callback As Silent"] = paramCasted.SelectToken("flagItemAddedCallbackAsSilent").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Is Player"] = paramCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Item ID"] = paramCasted.SelectToken("itemID.$value").Value<string>();

                        var itemToIgnore = "";
                        foreach (var p in paramCasted.SelectToken("itemIDsToIgnoreOnRemove"))
                        {
                            itemToIgnore += (itemToIgnore != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                        }
                        details["#" + counter + " Item IDs To Ignore On Remove"] = itemToIgnore;

                        details["#" + counter + " Node Type"] = paramCasted.SelectToken("nodeType").Value<string>();
                        details["#" + counter + " Object Ref"] = ParseGameEntityReference(paramCasted.SelectToken("objectRef"));
                        details["#" + counter + " Quantity"] = paramCasted.SelectToken("quantity").Value<string>();
                        details["#" + counter + " Remove All Quantity"] = paramCasted.SelectToken("removeAllQuantity").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Send Notification"] = paramCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";

                        var tagsToIgnore = "";
                        foreach (var p in paramCasted.SelectToken("tagsToIgnoreOnRemove"))
                        {
                            tagsToIgnore += (tagsToIgnore != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                        }
                        details["#" + counter + " Tags To Ignore On Remove"] = tagsToIgnore;

                        details["#" + counter + " Tag To Remove"] = paramCasted.SelectToken("tagToRemove.$value").Value<string>();

                        counter++;
                    }
                }
            }
            else if (nodeType == "questCrowdManagerNodeDefinition")
            {
                var crowdManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = crowdManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questCrowdManagerNodeType_ControlCrowd")
                {
                    details["Action"] = crowdManagerCasted.SelectToken("action").Value<string>();
                    details["Debug Source"] = crowdManagerCasted.SelectToken("debugSource.$value").Value<string>();
                    details["Distant Crowd Only"] = crowdManagerCasted.SelectToken("distantCrowdOnly").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questFXManagerNodeDefinition")
            {
                var fxManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = fxManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questPlayFX_NodeType")
                {
                    var paramsArr = fxManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        details["#" + counter + " Effect Instance Name"] = param.SelectToken("effectInstanceName.$value").Value<string>();
                        details["#" + counter + " Effect Name"] = param.SelectToken("effectName.$value").Value<string>();
                        details["#" + counter + " Is Player"] = param.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Object Ref"] = ParseGameEntityReference(param.SelectToken("objectRef"));
                        details["#" + counter + " Play"] = param.SelectToken("play").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Save"] = param.SelectToken("save").Value<string>() == "1" ? "True" : "False";
                        details["#" + counter + " Sequence Shift"] = param.SelectToken("sequenceShift").Value<string>();

                        counter++;
                    }
                }
            }
            else if (nodeType == "questRandomizerNodeDefinition")
            {
                details["Mode"] = node.SelectToken("mode").Value<string>();

                var outputWeights = "";
                foreach (var p in node.SelectToken("outputWeights"))
                {
                    outputWeights += (outputWeights != "" ? ", " : "") + p.ToString();
                }
                details["Output Weights"] = outputWeights;
            }
            else if (nodeType == "questEntityManagerNodeDefinition")
            {
                var entityManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = entityManagerCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questEntityManagerToggleMirrorsArea_NodeType")
                {
                    details["Is In Mirrors Area"] = entityManagerCasted.SelectToken("isInMirrorsArea").Value<string>() == "1" ? "True" : "False";
                    details["Object Ref"] = ParseGameEntityReference(entityManagerCasted.SelectToken("objectRef"));
                }
                if (nodeType2 == "questEntityManagerChangeAppearance_NodeType")
                {
                    details["Appearance Name"] = entityManagerCasted.SelectToken("appearanceName.$value").Value<string>();
                    details["Entity Ref"] = ParseGameEntityReference(entityManagerCasted.SelectToken("entityRef"));
                    details["Prefetch Only"] = entityManagerCasted.SelectToken("prefetchOnly").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questMovePuppetNodeDefinition")
            {
                details["Entity Reference"] = ParseGameEntityReference(node.SelectToken("entityReference"));
                details["Move Type"] = node.SelectToken("moveType.$value").Value<string>();

                string nodeType2 = node.SelectToken("nodeParams.Data.$type").Value<string>();

                if (nodeType2 == "questMoveOnSplineParams")
                {
                    var splineParamsCasted = node.SelectToken("nodeParams.Data");

                    details["Spline Node Ref"] = splineParamsCasted.SelectToken("splineNodeRef.$value").Value<string>();
                }
            }
            else if (nodeType == "questVehicleNodeDefinition")
            {
                var vehicleNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = vehicleNodeCasted.SelectToken("$type").Value<string>();

                details["Manager"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questMoveOnSpline_NodeType")
                {
                    details["Arrive With Pivot"] = vehicleNodeCasted.SelectToken("arriveWithPivot").Value<string>() == "1" ? "True" : "False";
                    details["Audio Curves"] = vehicleNodeCasted.SelectToken("audioCurves.DepotPath.$value").Value<string>();
                    details["Blend Time"] = vehicleNodeCasted.SelectToken("blendTime").Value<string>();
                    details["Blend Type"] = vehicleNodeCasted.SelectToken("blendType").Value<string>();
                    details["Overrides"] = vehicleNodeCasted.SelectToken("overrides") != null ? "Has overrides" : "";
                    details["Reverse Gear"] = vehicleNodeCasted.SelectToken("reverseGear").Value<string>() == "1" ? "True" : "False";
                    details["Scene Blend In Distance"] = vehicleNodeCasted.SelectToken("sceneBlendInDistance").Value<string>();
                    details["Scene Blend Out Distance"] = vehicleNodeCasted.SelectToken("sceneBlendOutDistance").Value<string>();
                    details["Spline Ref"] = vehicleNodeCasted.SelectToken("splineRef.$value").Value<string>();
                    details["Start From"] = vehicleNodeCasted.SelectToken("startFrom").Value<string>();
                    details["Traffic Deletion Mode"] = vehicleNodeCasted.SelectToken("trafficDeletionMode").Value<string>();
                    details["Vehicle Ref"] = ParseGameEntityReference(vehicleNodeCasted.SelectToken("vehicleRef"));
                }
                if (nodeType2 == "questTeleport_NodeType")
                {
                    details["Entity Reference"] = ParseGameEntityReference(vehicleNodeCasted.SelectToken("entityReference"));
                    details["Destination Offset"] = ParseVector(vehicleNodeCasted.SelectToken("params.destinationOffset"));
                    details["Destination Ref"] = GetNameFromUniversalRef(vehicleNodeCasted.SelectToken("params.destinationRef.Data"));
                }
            }
            else if (nodeType == "questCheckpointNodeDefinition")
            {
                var additionalEndGameRewardsTweak = "";
                foreach (var p in node.SelectToken("additionalEndGameRewardsTweak"))
                {
                    additionalEndGameRewardsTweak += (additionalEndGameRewardsTweak != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                }
                details["Additional End Game Rewards Tweak"] = additionalEndGameRewardsTweak;

                details["Debug String"] = node.SelectToken("debugString").Value<string>();
                details["End Game Save"] = node.SelectToken("endGameSave").Value<string>() == "1" ? "True" : "False";
                details["Ignore Save Locks"] = node.SelectToken("ignoreSaveLocks").Value<string>() == "1" ? "True" : "False";
                details["Point Of No Return"] = node.SelectToken("pointOfNoReturn").Value<string>() == "1" ? "True" : "False";
                details["Retry On Failure"] = node.SelectToken("retryOnFailure").Value<string>() == "1" ? "True" : "False";
                details["Save Lock"] = node.SelectToken("saveLock").Value<string>() == "1" ? "True" : "False";
            }

            return details;
        }

        public static NodeProps GetPropertiesForSectionNode(JToken node, JToken scnSceneResource = null)
        {
            NodeProps details = new();

            string nodeType = node.SelectToken("$type").Value<string>();

            details["Type"] = GetNameFromClass(nodeType);

            if (nodeType == "scnRewindableSectionNode")
            {
                details["Play Speed Modifiers - Backward Fast"] = node.SelectToken("playSpeedModifiers.backwardFast").Value<string>();
                details["Play Speed Modifiers - Backward Slow"] = node.SelectToken("playSpeedModifiers.backwardSlow").Value<string>();
                details["Play Speed Modifiers - Backward Very Fast"] = node.SelectToken("playSpeedModifiers.backwardVeryFast").Value<string>();
                details["Play Speed Modifiers - Forward Fast"] = node.SelectToken("playSpeedModifiers.forwardFast").Value<string>();
                details["Play Speed Modifiers - Forward Slow"] = node.SelectToken("playSpeedModifiers.forwardSlow").Value<string>();
                details["Play Speed Modifiers - Forward Very Fast"] = node.SelectToken("playSpeedModifiers.forwardVeryFast").Value<string>();
            }
            if (nodeType == "scnSectionNode" || nodeType == "scnRewindableSectionNode")
            {
                details["Section Duration"] = node.SelectToken("sectionDuration.stu").Value<string>() + "ms";
                details["Ff Strategy"] = node.SelectToken("ffStrategy").Value<string>();

                var events = node.SelectToken("events");
                details["Events"] = events.Count().ToString();

                int counter = 1;
                foreach (var eventClass in events)
                {
                    NodeProps subProps = new();
                    string evName = eventClass.SelectToken("Data.$type").Value<string>();

                    if (evName == "scneventsSocket")
                    {
                        evName += " - Name: " + eventClass.SelectToken("Data.osockStamp.name").Value<string>() + ", Ordinal: " + eventClass.SelectToken("Data.osockStamp.ordinal").Value<string>();
                    }
                    else if (evName == "scnPlaySkAnimEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);

                        var animType = eventClass.SelectToken("Data.animName.Data.type").Value<string>();
                        if (animType == "direct")
                            subProps["Anim"] = eventClass?.SelectToken("Data.animName.Data.unk1[0].$value")?.Value<string>();
                    }
                    else if (evName == "scnAudioEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);
                        subProps["Event"] = eventClass.SelectToken("Data.audioEventName.$value").Value<string>();
                    }
                    else if (evName == "scnDialogLineEvent")
                    {
                        evName += " - " + GetScreenplayItem(eventClass.SelectToken("Data.screenplayLineId.id").Value<string>(), scnSceneResource);
                    }
                    else if (evName == "scnLookAtEvent")
                    {
                        subProps["Is Start"] = eventClass.SelectToken("Data.basicData.basic.isStart").Value<string>() == "1" ? "True" : "False";
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.basicData.basic.performerId.id").Value<string>(), scnSceneResource);
                        subProps["Static Target"] = ParseVector(eventClass.SelectToken("Data.basicData.basic.staticTarget"));
                        subProps["Target Performer"] = GetPerformer(eventClass.SelectToken("Data.basicData.basic.targetPerformerId.id").Value<string>(), scnSceneResource);
                    }

                    details["#" + counter.ToString() + " " + eventClass.SelectToken("Data.startTime").Value<string>() + "ms" + " ET:" + eventClass.SelectToken("Data.executionTagFlags").Value<string>(), evName] = subProps;
                    counter++;
                }
            }
            if (nodeType == "scnStartNode")
            {
                foreach (var e in scnSceneResource.SelectToken("entryPoints"))
                {
                    if (e.SelectToken("nodeId.id").Value<string>() == node.SelectToken("nodeId.id").Value<string>())
                    {
                        details["Start name"] = e.SelectToken("name.$value").Value<string>();
                        break;
                    }
                }
            }
            if (nodeType == "scnEndNode")
            {
                details["Type"] = node.SelectToken("type").Value<string>();

                foreach (var e in scnSceneResource.SelectToken("exitPoints"))
                {
                    if (e.SelectToken("nodeId.id").Value<string>() == node.SelectToken("nodeId.id").Value<string>())
                    {
                        details["Exit name"] = e.SelectToken("name.$value").Value<string>();
                        break;
                    }
                }
            }
            if (nodeType == "scnRandomizerNode")
            {
                details["FF Strategy"] = node.SelectToken("ffStrategy").Value<string>();
                details["Mode"] = node.SelectToken("mode").Value<string>();

                var outputWeights = "";
                foreach (var p in node.SelectToken("weights.Elements"))
                {
                    outputWeights += (outputWeights != "" ? ", " : "") + p.ToString();
                }
                details["Weights"] = outputWeights;
            }
            if (nodeType == "scnChoiceNode")
            {
                var mode = node.SelectToken("mode").Value<string>();

                details["Mode"] = mode;

                if (mode == "attachToActor")
                {
                    details["Actor ID"] = node.SelectToken("ataParams.actorId.id").Value<string>();
                }
                if (mode == "attachToGameObject")
                {
                    details["Node Ref"] = node.SelectToken("atgoParams.nodeRef.$value").Value<string>();
                }
                if (mode == "attachToProp")
                {
                    details["Prop ID"] = node.SelectToken("atpParams.propId.id").Value<string>();
                }
                if (mode == "attachToScreen")
                {
                }
                if (mode == "attachToWorld")
                {
                    details["Custom Entity Radius"] = node.SelectToken("atwParams.customEntityRadius").Value<string>() == "1" ? "True" : "False";
                    details["Entity Orientation"] = ParseQuaternion(node.SelectToken("atwParams.entityOrientation"));
                    details["Entity Position"] = ParseVector(node.SelectToken("atwParams.entityPosition"));
                    details["Visualizer Style"] = node.SelectToken("atwParams.visualizerStyle").Value<string>();
                }

                details["Display Name Override"] = node.SelectToken("displayNameOverride").Value<string>();
                details["Hub Priority"] = node.SelectToken("hubPriority").Value<string>();
                details["Choice Group"] = node.SelectToken("choiceGroup.$value").Value<string>();

                var optionsArr = node.SelectToken("options");

                int counter = 1;
                foreach (var option in optionsArr)
                {
                    NodeProps subProps = new();

                    subProps["Caption"] = option.SelectToken("caption.$value").Value<string>();
                    subProps["Type Properties"] = option.SelectToken("type.properties").Value<string>();

                    var iconTagIds = "";
                    foreach (var p in option.SelectToken("iconTagIds"))
                    {
                        iconTagIds += (iconTagIds != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                    }
                    subProps["Icon Tag Ids"] = iconTagIds;

                    NodeProps subPropsConds = GetPropertiesForConditions(option.SelectToken("questCondition.Data"));
                    subProps["Quest Condition", ""] = subPropsConds;

                    details["Option #" + counter, GetScreenplayOption(option.SelectToken("screenplayOptionId.id").Value<string>(), scnSceneResource)] = subProps;
                    counter++;
                }

                if (node.SelectToken("shapeParams.Data") != null)
                {
                    details["Shape Params - Activation Base Length"] = node.SelectToken("shapeParams.Data.activationBaseLength").Value<string>();
                    details["Shape Params - Activation Height"] = node.SelectToken("shapeParams.Data.activationHeight").Value<string>();
                    details["Shape Params - Activation Yaw Limit"] = node.SelectToken("shapeParams.Data.activationYawLimit").Value<string>();
                    details["Shape Params - Custom Activation Range"] = node.SelectToken("shapeParams.Data.customActivationRange").Value<string>();
                    details["Shape Params - Custom Indication Range"] = node.SelectToken("shapeParams.Data.customIndicationRange").Value<string>();
                }
            }

            return details;
        }

        private static NodeProps GetPropertiesForConditions(JToken node, string logicalCondIndex = "")
        {
            NodeProps details = new();

            if (node == null) return details;

            string nodeType = node.SelectToken("$type").Value<string>();

            details[logicalCondIndex + "Condition type"] = GetNameFromClass(nodeType);

            if (nodeType == "questTimeCondition")
            {
                var condTimeCasted = node.SelectToken("type.Data");

                string nodeType2 = condTimeCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                //string ConditionTimeTypeName = logicalCondIndex + "Time condition";
                string ConditionTimeValTypeName = logicalCondIndex + "Time";

                if (nodeType2 == "questRealtimeDelay_ConditionType")
                {
                    //details[ConditionTimeTypeName] = GetNameFromClass(nodeRealtimeCondCasted);
                    details[ConditionTimeValTypeName] =
                        FormatNumber(condTimeCasted.SelectToken("hours").Value<uint>()) + "h:" +
                        FormatNumber(condTimeCasted.SelectToken("minutes").Value<uint>()) + "m:" +
                        FormatNumber(condTimeCasted.SelectToken("seconds").Value<uint>()) + "s:" +
                        FormatNumber(condTimeCasted.SelectToken("miliseconds").Value<uint>(), true) + "ms";
                }

                if (nodeType2 == "questGameTimeDelay_ConditionType")
                {
                    details[ConditionTimeValTypeName] =
                        FormatNumber(condTimeCasted.SelectToken("days").Value<uint>()) + "d " +
                        FormatNumber(condTimeCasted.SelectToken("hours").Value<uint>()) + "h:" +
                        FormatNumber(condTimeCasted.SelectToken("minutes").Value<uint>()) + "m:" +
                        FormatNumber(condTimeCasted.SelectToken("seconds").Value<uint>()) + "s";
                }

                if (nodeType2 == "questTickDelay_ConditionType")
                {
                    details[ConditionTimeValTypeName] = FormatNumber(condTimeCasted.SelectToken("tickCount").Value<uint>()) + " ticks";
                }

                if (nodeType2 == "questTimePeriod_ConditionType")
                {
                    string timeNameFrom = "";
                    string timeNameTo = "";

                    TimeSpan t = TimeSpan.FromSeconds(condTimeCasted.SelectToken("begin.seconds").Value<double>());
                    timeNameFrom = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds);

                    TimeSpan t2 = TimeSpan.FromSeconds(condTimeCasted.SelectToken("end.seconds").Value<double>());
                    timeNameTo = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t2.Hours, t2.Minutes, t2.Seconds);

                    details[ConditionTimeValTypeName] =
                        "from " + FormatNumber(condTimeCasted.SelectToken("begin.seconds").Value<uint>()) + " (" + timeNameFrom + ")" +
                        " to " + FormatNumber(condTimeCasted.SelectToken("end.seconds").Value<uint>()) + " (" + timeNameTo + ")";
                }
            }
            else if (nodeType == "questFactsDBCondition")
            {
                var condFactCasted = node.SelectToken("type.Data");

                string nodeType2 = condFactCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questVarComparison_ConditionType")
                {
                    details[logicalCondIndex + "Fact Name"] = condFactCasted.SelectToken("factName").Value<string>();
                    details[logicalCondIndex + "Comparison Type"] = condFactCasted.SelectToken("comparisonType").Value<string>();
                    details[logicalCondIndex + "Value"] = condFactCasted.SelectToken("value").Value<string>();
                }

                if (nodeType2 == "questVarVsVarComparison_ConditionType")
                {
                    details[logicalCondIndex + "Comparison Type"] = condFactCasted.SelectToken("comparisonType").Value<string>();
                    details[logicalCondIndex + "Fact 1 Name"] = condFactCasted.SelectToken("factName1").Value<string>();
                    details[logicalCondIndex + "Fact 2 Name"] = condFactCasted.SelectToken("factName2").Value<string>();
                }
            }
            else if (nodeType == "questLogicalCondition")
            {
                //details[logicalCondIndex + "Operation"] = node.SelectToken("operation").Value<string>();

                NodeProps subProps = new();

                var conditions = node.SelectToken("conditions");
                for (int i = 0; i < conditions.Count(); i++)
                {
                    subProps.AddRange(GetPropertiesForConditions(conditions[i].SelectToken("Data"), logicalCondIndex + "#" + i + " "));
                }
                
                details[logicalCondIndex + "Operation", node.SelectToken("operation").Value<string>()] = subProps;
            }
            else if (nodeType == "questCharacterCondition")
            {
                var condCharacterCasted = node.SelectToken("type.Data");

                string nodeType2 = condCharacterCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questCharacterBodyType_CondtionType")
                {
                    details[logicalCondIndex + "Gender"] = condCharacterCasted.SelectToken("gender.$value").Value<string>();
                    details[logicalCondIndex + "Object Ref"] = ParseGameEntityReference(condCharacterCasted.SelectToken("objectRef"));
                    details[logicalCondIndex + "Is Player"] = condCharacterCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questCharacterSpawned_ConditionType")
                {
                    details[logicalCondIndex + "Comparison Type"] = condCharacterCasted.SelectToken("comparisonParams.Data.comparisonType").Value<string>();
                    details[logicalCondIndex + "Comparison Count"] = condCharacterCasted.SelectToken("comparisonParams.Data.count").Value<string>();
                    details[logicalCondIndex + "Comparison Entire Community"] = condCharacterCasted.SelectToken("comparisonParams.Data.entireCommunity").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Object Ref"] = ParseGameEntityReference(condCharacterCasted.SelectToken("objectRef"));
                }
                if (nodeType2 == "questCharacterKilled_ConditionType")
                {
                    details[logicalCondIndex + "Comparison Type"] = condCharacterCasted.SelectToken("comparisonParams.Data.comparisonType").Value<string>();
                    details[logicalCondIndex + "Comparison Count"] = condCharacterCasted.SelectToken("comparisonParams.Data.count").Value<string>();
                    details[logicalCondIndex + "Comparison Entire Community"] = condCharacterCasted.SelectToken("comparisonParams.Data.entireCommunity").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Defeated"] = condCharacterCasted.SelectToken("defeated").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Killed"] = condCharacterCasted.SelectToken("killed").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Object Ref"] = ParseGameEntityReference(condCharacterCasted.SelectToken("objectRef"));
                    details[logicalCondIndex + "Source"] = GetNameFromUniversalRef(condCharacterCasted.SelectToken("source"));
                    details[logicalCondIndex + "Unconscious"] = condCharacterCasted.SelectToken("unconscious").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questTriggerCondition")
            {
                details[logicalCondIndex + "Activator Ref"] = ParseGameEntityReference(node.SelectToken("activatorRef"));
                details[logicalCondIndex + "Is Player Activator"] = node.SelectToken("isPlayerActivator").Value<string>() == "1" ? "True" : "False";
                details[logicalCondIndex + "Trigger Area Ref"] = node.SelectToken("triggerAreaRef.$value").Value<string>();
                details[logicalCondIndex + "Trigger Type"] = node.SelectToken("type").Value<string>();
            }
            else if (nodeType == "questSystemCondition")
            {
                var condSystemCasted = node.SelectToken("type.Data");

                string nodeType2 = condSystemCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questCameraFocus_ConditionType")
                {
                    details[logicalCondIndex + "Angle Tolerance"] = condSystemCasted.SelectToken("angleTolerance").Value<string>();
                    details[logicalCondIndex + "Inverted"] = condSystemCasted.SelectToken("inverted").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Object Ref"] = ParseGameEntityReference(condSystemCasted.SelectToken("objectRef"));
                    details[logicalCondIndex + "On Screen Test"] = condSystemCasted.SelectToken("onScreenTest").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Time Interval"] = condSystemCasted.SelectToken("timeInterval").Value<string>();
                    details[logicalCondIndex + "Use Frustrum Check"] = condSystemCasted.SelectToken("useFrustrumCheck").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Zoomed"] = condSystemCasted.SelectToken("zoomed").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questInputAction_ConditionType")
                {
                    details[logicalCondIndex + "Any Input Action"] = condSystemCasted.SelectToken("anyInputAction").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Axis Action"] = condSystemCasted.SelectToken("axisAction").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Check If Button Already Pressed"] = condSystemCasted.SelectToken("checkIfButtonAlreadyPressed").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Input Action"] = condSystemCasted.SelectToken("inputAction.$value").Value<string>();
                    details[logicalCondIndex + "Value Less Than"] = condSystemCasted.SelectToken("valueLessThan").Value<string>();
                    details[logicalCondIndex + "Value More Than"] = condSystemCasted.SelectToken("valueMoreThan").Value<string>();
                }
                if (nodeType2 == "questPrereq_ConditionType")
                {
                    details[logicalCondIndex + "Is Object Player"] = condSystemCasted.SelectToken("isObjectPlayer").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Object Ref"] = ParseGameEntityReference(condSystemCasted.SelectToken("objectRef"));

                    var prereq = condSystemCasted.SelectToken("prereq.Data");
                    var type = prereq.SelectToken("$type").Value<string>();

                    details[logicalCondIndex + "Prereq"] = type;

                    if (type == "DialogueChoiceHubPrereq")
                        details[logicalCondIndex + "Is Choice Hub Active"] = prereq.SelectToken("isChoiceHubActive").Value<string>();

                    if (type == "PlayerControlsDevicePrereq")
                        details[logicalCondIndex + "Inverse"] = prereq.SelectToken("inverse").Value<string>() == "1" ? "True" : "False";
                }
            }
            else if (nodeType == "questDistanceCondition")
            {
                var condDistanceCasted = node.SelectToken("type.Data");

                string nodeType2 = condDistanceCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questDistanceComparison_ConditionType")
                {
                    details[logicalCondIndex + "Comparison Type"] = condDistanceCasted.SelectToken("comparisonType").Value<string>();
                    details[logicalCondIndex + "Entity Ref"] = GetNameFromUniversalRef(condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data"));
                    //details[logicalCondIndex + "Entity Ref - Entity Reference"] = GetNameFromUniversalRef(condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data"));
                    //details[logicalCondIndex + "Entity Ref - Main Player Object"] = condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data.mainPlayerObject").Value<string>() == "1" ? "True" : "False";
                    //details[logicalCondIndex + "Entity Ref - Ref Local Player"] = condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data.refLocalPlayer").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Node Ref 2"] = ParseGameEntityReference(condDistanceCasted.SelectToken("distanceDefinition1.Data.nodeRef2"));
                    details[logicalCondIndex + "Distance Value"] = condDistanceCasted.SelectToken("distanceDefinition2.Data.distanceValue").Value<string>();
                }
            }
            else if (nodeType == "questSceneCondition")
            {
                var condSceneCasted = node.SelectToken("type.Data");

                string nodeType2 = condSceneCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questSectionNode_ConditionType")
                {
                    details[logicalCondIndex + "Scene File"] = condSceneCasted.SelectToken("sceneFile.DepotPath.$value").Value<string>();
                    details[logicalCondIndex + "Scene Version"] = condSceneCasted.SelectToken("SceneVersion").Value<string>();
                    details[logicalCondIndex + "Section Name"] = condSceneCasted.SelectToken("sectionName.$value").Value<string>();
                    details[logicalCondIndex + "Cond Type"] = condSceneCasted.SelectToken("type").Value<string>();
                }
            }
            else if (nodeType == "questJournalCondition")
            {
                var journalCasted = node.SelectToken("type.Data");

                string nodeType2 = journalCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questJournalEntryState_ConditionType")
                {
                    details[logicalCondIndex + "Inverted"] = journalCasted.SelectToken("inverted").Value<string>() == "1" ? "True" : "False";
                    details.AddRange(ParseJournalPath(journalCasted.SelectToken("path.Data"), logicalCondIndex));
                    details[logicalCondIndex + "State"] = journalCasted.SelectToken("state").Value<string>();
                }
            }
            else if (nodeType == "questObjectCondition")
            {
                var objectCasted = node.SelectToken("type.Data");

                string nodeType2 = objectCasted.SelectToken("$type").Value<string>();

                details[logicalCondIndex + "Condition subtype"] = GetNameFromClass(nodeType2);

                if (nodeType2 == "questInventory_ConditionType")
                {
                    details[logicalCondIndex + "Comparison Type"] = objectCasted.SelectToken("comparisonType").Value<string>();
                    details[logicalCondIndex + "Is Player"] = objectCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    details[logicalCondIndex + "Item ID"] = objectCasted.SelectToken("itemID.$value").Value<string>();
                    details[logicalCondIndex + "Item Tag"] = objectCasted.SelectToken("itemTag.$value").Value<string>();
                    details[logicalCondIndex + "Object Ref"] = ParseGameEntityReference(objectCasted.SelectToken("objectRef"));
                    details[logicalCondIndex + "Quantity"] = objectCasted.SelectToken("quantity").Value<string>();
                }
            }

            return details;
        }

        private static string GetScreenplayOption(string screenplayOptionID, JToken scnSceneResource)
        {
            string retVal = "OptionID: " + screenplayOptionID + ", ";

            if (scnSceneResource != null)
            {
                foreach (var screenplayStoreOptions in scnSceneResource.SelectToken("screenplayStore.options"))
                {
                    if (screenplayStoreOptions.SelectToken("itemId.id").Value<string>() == screenplayOptionID)
                    {
                        retVal += "LocStringID: " + screenplayStoreOptions.SelectToken("locstringId.ruid").Value<string>();
                        break;
                    }
                }
            }

            return retVal;
        }

        private static string GetScreenplayItem(string screenplayItemID, JToken scnSceneResource)
        {
            string retVal = "ItemID: " + screenplayItemID + ", ";

            if (scnSceneResource != null)
            {
                foreach (var screenplayStoreLines in scnSceneResource.SelectToken("screenplayStore.lines"))
                {
                    if (screenplayStoreLines.SelectToken("itemId.id").Value<string>() == screenplayItemID)
                    {
                        retVal += "LocStringID: " + screenplayStoreLines.SelectToken("locstringId.ruid").Value<string>();
                        break;
                    }
                }
            }

            return retVal;
        }

        private static string GetPerformer(string performerID, JToken scnSceneResource)
        {
            string retVal = "(" + performerID + ") ";

            if (scnSceneResource != null)
            {
                foreach (var performersDebugSymbols in scnSceneResource.SelectToken("debugSymbols.performersDebugSymbols"))
                {
                    if (performersDebugSymbols.SelectToken("performerId.id").Value<string>() == performerID)
                    {
                        retVal += ParseGameEntityReference(performersDebugSymbols.SelectToken("entityRef"));
                        break;
                    }
                }
            }

            return retVal;
        }

        private static string ParseQuaternion(JToken vec)
        {
            if (vec == null)
                return "-";

            var i = vec.SelectToken("i").Value<string>();
            var j = vec.SelectToken("j").Value<string>();
            var k = vec.SelectToken("k").Value<string>();
            var r = vec.SelectToken("r").Value<string>();

            return "I: " + i + ", J: " + j + ", K: " + k + ", R: " + r;
        }

        private static string ParseVector(JToken vec)
        {
            if (vec == null)
                return "-";

            var x = vec.SelectToken("X").Value<string>();
            var y = vec.SelectToken("Y").Value<string>();
            var z = vec.SelectToken("Z").Value<string>();

            return "X: " + x + ", Y: " + y + ", Z: " + z;
        }

        private static string ParseMargin(JToken margin)
        {
            string str = "-";
            if (margin != null)
            {
                str = "Left: " + margin.SelectToken("left").Value<string>() + ", ";
                str += "Top: " + margin.SelectToken("top").Value<string>() + ", ";
                str += "Right: " + margin.SelectToken("right").Value<string>() + ", ";
                str += "Bottom: " + margin.SelectToken("bottom").Value<string>();
            }
            return str;
        }

        private static string ParseColor(JToken clr)
        {
            var alpha = clr.SelectToken("Alpha").Value<string>();
            var blue = clr.SelectToken("Blue").Value<string>();
            var green = clr.SelectToken("Green").Value<string>();
            var red = clr.SelectToken("Red").Value<string>();

            return "Alpha: " + alpha + ", Blue: " + blue + ", Green: " + green + ", Red: " + red;
        }

        private static NodeProps ParseBDMask(JToken mask)
        {
            NodeProps details = new();
            details["Camera Toggle Action"] = mask.SelectToken("cameraToggleAction").Value<string>() == "1" ? "True" : "False";
            details["Pause Action"] = mask.SelectToken("pauseAction").Value<string>() == "1" ? "True" : "False";
            details["Play Backward Action"] = mask.SelectToken("playBackwardAction").Value<string>() == "1" ? "True" : "False";
            details["Play Forward Action"] = mask.SelectToken("playForwardAction").Value<string>() == "1" ? "True" : "False";
            details["Restart Action"] = mask.SelectToken("restartAction").Value<string>() == "1" ? "True" : "False";
            details["Switch Layer Action"] = mask.SelectToken("switchLayerAction").Value<string>() == "1" ? "True" : "False";
            return details;
        }

        private static string GetWorkspotPath(string workspotID, JToken scnSceneResource)
        {
            string retVal = "";

            if (scnSceneResource != null)
            {
                string dataID = "0";

                foreach (var workspotInstance in scnSceneResource.SelectToken("workspotInstances"))
                {
                    if (workspotInstance.SelectToken("workspotInstanceId.id").Value<string>() == workspotID)
                    {
                        dataID = workspotInstance.SelectToken("dataId.id").Value<string>();
                        break;
                    }
                }

                if (dataID != "0")
                {
                    foreach (var workspot in scnSceneResource.SelectToken("workspots"))
                    {
                        var workspotData = workspot.SelectToken("Data");

                        if (workspotData.SelectToken("$type").Value<string>() == "scnWorkspotData_ExternalWorkspotResource")
                        {
                            if (workspotData.SelectToken("dataId.id").Value<string>() == dataID)
                            {
                                retVal = Path.GetFileName(workspotData.SelectToken("workspotResource.DepotPath.$value").Value<string>());
                                break;
                            }
                        }
                    }
                }
            }

            return retVal;
        }

        private static NodeProps ParseJournalPath(JToken gameJournalPath, string possiblePrefix = "")
        {
            NodeProps details = new();
            details[possiblePrefix + "Path Class Name"] = gameJournalPath.SelectToken("className.$value")?.Value<string>();
            details[possiblePrefix + "Path File Entry Index"] = gameJournalPath.SelectToken("fileEntryIndex")?.Value<string>();
            details[possiblePrefix + "Path Real Path"] = gameJournalPath.SelectToken("realPath")?.Value<string>();
            return details;
        }

        private static string ParseGameEntityReference(JToken entRef)
        {
            string str = "-";

            if (entRef?.SelectToken("dynamicEntityUniqueName.$value")?.Value<string>() != "None")
            {
                str = entRef?.SelectToken("dynamicEntityUniqueName.$value")?.Value<string>();
            }
            if (entRef?.SelectToken("reference.$value")?.Value<string>() != "0")
            {
                str = entRef?.SelectToken("reference.$value")?.Value<string>();
            }

            if (entRef?.SelectToken("names")?.Count() > 0)
            {
                string names = "";
                foreach (var name in entRef.SelectToken("names"))
                {
                    names += (names == "" ? "" : ", ") + name.SelectToken("$value").Value<string>();
                }

                str += " [" + names + "]";
            }

            return str;
        }

        private static string GetNameFromUniversalRef(JToken uniRef)
        {
            string outStr = "";

            string entRef = ParseGameEntityReference(uniRef?.SelectToken("entityReference"));
            if (entRef == "-")
            {
                outStr = uniRef.SelectToken("refLocalPlayer").Value<string>() == "1" ? "Ref Local Player" : "";
                outStr += uniRef.SelectToken("mainPlayerObject").Value<string>() == "1" ? ((outStr != "" ? ", " : "") + "Main Player Object") : "";
            }
            else
            {
                outStr = entRef;
            }

            return outStr;
        }

        private static string GetNameFromClass(string nodeQuestType)
        {
            string name = nodeQuestType;

            name = name.Replace("quest", "");
            name = name.Replace("_ConditionType", "");
            name = name.Replace("NodeDefinition", "");
            name = name.Replace("_NodeType", "");
            name = name.Replace("_NodeSubType", "");

            return name;
        }

        private static string FormatNumber(UInt32? number, bool three = false)
        {
            if (number < 10 && !three)
            {
                return "0" + (number.ToString() ?? "0");
            }
            else if (number < 10 && three)
            {
                return "00" + (number.ToString() ?? "0");
            }
            else if (number < 100 && three)
            {
                return "0" + (number.ToString() ?? "0");
            }
            else
            {
                return (number.ToString() ?? (three ? "000" : "00"));
            }
        }

        private static string FormatNumber(Int32? number, bool three = false)
        {
            if (number < 10 && !three)
            {
                return "0" + (number.ToString() ?? "0");
            }
            else if (number < 10 && three)
            {
                return "00" + (number.ToString() ?? "0");
            }
            else if (number < 100 && three)
            {
                return "0" + (number.ToString() ?? "0");
            }
            else
            {
                return (number.ToString() ?? (three ? "000" : "00"));
            }
        }
    }

    public static class CollectionHelper
    {
        public static void AddRangeOverride<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
        {
            dicToAdd.ForEach(x => dic[x.Key] = x.Value);
        }

        public static void AddRangeNewOnly<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
        {
            dicToAdd.ForEach(x => { if (!dic.ContainsKey(x.Key)) dic.Add(x.Key, x.Value); });
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic, IDictionary<TKey, TValue> dicToAdd)
        {
            dicToAdd.ForEach(x => dic.Add(x.Key, x.Value));
        }

        public static bool ContainsKeys<TKey, TValue>(this IDictionary<TKey, TValue> dic, IEnumerable<TKey> keys)
        {
            bool result = false;
            keys.ForEachOrBreak((x) => { result = dic.ContainsKey(x); return result; });
            return result;
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (var item in source)
                action(item);
        }

        public static void ForEachOrBreak<T>(this IEnumerable<T> source, Func<T, bool> func)
        {
            foreach (var item in source)
            {
                bool result = func(item);
                if (result) break;
            }
        }
    }
}
