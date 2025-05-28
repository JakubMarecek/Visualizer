using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;

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

            details["Quest Node Type"] = GetNameFromClass(nodeType);

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

                NodeProps subProps = new();

                if (nodeType2 == "questSetVar_NodeType")
                {
                    subProps["Fact Name"] = factDBManagerCasted.SelectToken("factName").Value<string>();
                    subProps["Set Exact Value"] = factDBManagerCasted.SelectToken("setExactValue").Value<string>() == "1" ? "True" : "False";
                    subProps["Value"] = factDBManagerCasted.SelectToken("value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questJournalNodeDefinition")
            {
                var journalNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = journalNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questJournalQuestEntry_NodeType")
                {
                    subProps.AddRange(ParseJournalPath(journalNodeCasted.SelectToken("path.Data")));

                    subProps["Send Notification"] = journalNodeCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";
                    subProps["Track Quest"] = journalNodeCasted.SelectToken("trackQuest").Value<string>() == "1" ? "True" : "False";
                    subProps["Version"] = journalNodeCasted.SelectToken("version").Value<string>();
                }
                if (nodeType2 == "questJournalEntry_NodeType")
                {
                    subProps.AddRange(ParseJournalPath(journalNodeCasted.SelectToken("path.Data")));

                    subProps["Send Notification"] = journalNodeCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questJournalBulkUpdate_NodeType")
                {
                    subProps["New Entry State"] = journalNodeCasted.SelectToken("newEntryState.$value").Value<string>();
                    subProps.AddRange(ParseJournalPath(journalNodeCasted.SelectToken("path.Data")));
                    subProps["Propagate Change"] = journalNodeCasted.SelectToken("propagateChange").Value<string>() == "1" ? "True" : "False";
                    subProps["Required Entry State"] = journalNodeCasted.SelectToken("requiredEntryState.$value").Value<string>();
                    subProps["Required Entry Type"] = journalNodeCasted.SelectToken("requiredEntryType.$value").Value<string>();
                    subProps["Send Notification"] = journalNodeCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questUseWorkspotNodeDefinition")
            {
                var useWorkspotNodeCasted = node.SelectToken("paramsV1.Data");

                details["Entity Reference"] = ParseGameEntityReference(node.SelectToken("entityReference"));

                string nodeType2 = useWorkspotNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "scnUseSceneWorkspotParamsV1")
                {
                    subProps["Entry Id"] = useWorkspotNodeCasted.SelectToken("entryId.id").Value<string>();
                    subProps["Exit Entry Id"] = useWorkspotNodeCasted.SelectToken("exitEntryId.id").Value<string>();
                    subProps["Workspot Instance Id"] = useWorkspotNodeCasted.SelectToken("workspotInstanceId.id").Value<string>();
                    subProps["Workspot Name"] = GetWorkspotPath(useWorkspotNodeCasted.SelectToken("workspotInstanceId.id").Value<string>(), scnSceneResource);
                }
                else if (nodeType2 == "questUseWorkspotParamsV1")
                {
                    subProps["Entry Id"] = useWorkspotNodeCasted.SelectToken("entryId.id").Value<string>();
                    subProps["Exit Entry Id"] = useWorkspotNodeCasted.SelectToken("exitEntryId.id").Value<string>();
                    subProps["Workspot Node"] = useWorkspotNodeCasted.SelectToken("workspotNode.$value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questSceneManagerNodeDefinition")
            {
                var sceneManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = sceneManagerNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questSetTier_NodeType")
                {
                    subProps["Force Empty Hands"] = sceneManagerNodeCasted.SelectToken("forceEmptyHands").Value<string>() == "1" ? "True" : "False";
                    subProps["Tier"] = sceneManagerNodeCasted.SelectToken("tier").Value<string>();
                }
                if (nodeType2 == "questCameraClippingPlane_NodeType")
                {
                    subProps["Preset"] = sceneManagerNodeCasted.SelectToken("preset").Value<string>();
                }
                if (nodeType2 == "questToggleEventExecutionTag_NodeType")
                {
                    subProps["Event Execution Tag"] = sceneManagerNodeCasted.SelectToken("eventExecutionTag.$value").Value<string>();
                    subProps["Mute"] = sceneManagerNodeCasted.SelectToken("mute").Value<string>() == "1" ? "True" : "False";
                    subProps["Scene File"] = sceneManagerNodeCasted.SelectToken("sceneFile.DepotPath.$value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questTimeManagerNodeDefinition")
            {
                var timeManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = timeManagerNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questPauseTime_NodeType")
                {
                    subProps["Pause"] = timeManagerNodeCasted.SelectToken("pause").Value<string>() == "1" ? "True" : "False";
                    subProps["Source"] = timeManagerNodeCasted.SelectToken("source.$value").Value<string>();
                }
                if (nodeType2 == "questSetTime_NodeType")
                {
                    subProps["Hours"] = timeManagerNodeCasted.SelectToken("hours").Value<string>();
                    subProps["Minutes"] = timeManagerNodeCasted.SelectToken("minutes").Value<string>();
                    subProps["Seconds"] = timeManagerNodeCasted.SelectToken("seconds").Value<string>();
                    subProps["Source"] = timeManagerNodeCasted.SelectToken("source.$value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questAudioNodeDefinition")
            {
                var audioNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = audioNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questAudioMixNodeType")
                {
                    subProps["Mix Signpost"] = audioNodeCasted.SelectToken("mixSignpost.$value").Value<string>();
                }
                if (nodeType2 == "questAudioEventNodeType")
                {
                    subProps["Ambient Unique Name"] = audioNodeCasted.SelectToken("ambientUniqueName.$value").Value<string>();

                    var dynamicParams = "";
                    foreach (var p in audioNodeCasted.SelectToken("dynamicParams"))
                    {
                        dynamicParams += (dynamicParams != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                    }
                    subProps["Dynamic Params"] = dynamicParams;

                    subProps["Emitter"] = audioNodeCasted.SelectToken("emitter.$value").Value<string>();
                    subProps["Event"] = audioNodeCasted.SelectToken("event.event.$value").Value<string>();

                    var events = "";
                    foreach (var p in audioNodeCasted.SelectToken("events"))
                    {
                        events += (events != "" ? ", " : "") + p.SelectToken("event.$value").Value<string>();
                    }
                    subProps["Events"] = events;

                    subProps["Is Music"] = audioNodeCasted.SelectToken("isMusic").Value<string>() == "1" ? "True" : "False";
                    subProps["Is Player"] = audioNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";

                    var musicEvents = "";
                    foreach (var p in audioNodeCasted.SelectToken("musicEvents"))
                    {
                        musicEvents += (musicEvents != "" ? ", " : "") + p.SelectToken("event.$value").Value<string>();
                    }
                    subProps["Music Events"] = musicEvents;

                    subProps["Object Ref"] = ParseGameEntityReference(audioNodeCasted.SelectToken("objectRef"));

                    var paramsStr = "";
                    foreach (var p in audioNodeCasted.SelectToken("params"))
                    {
                        paramsStr += (paramsStr != "" ? ", " : "") + p.SelectToken("name.$value").Value<string>();
                    }
                    subProps["Params"] = paramsStr;

                    var switches = "";
                    foreach (var p in audioNodeCasted.SelectToken("switches"))
                    {
                        switches += (switches != "" ? ", " : "") + p.SelectToken("name.$value").Value<string>();
                    }
                    subProps["Switches"] = switches;
                }
                if (nodeType2 == "questAudioSwitchNodeType")
                {
                    subProps["Is Music"] = audioNodeCasted.SelectToken("isMusic").Value<string>() == "1" ? "True" : "False";
                    subProps["Is Player"] = audioNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    subProps["Object Ref"] = ParseGameEntityReference(audioNodeCasted.SelectToken("objectRef"));
                    subProps["Switch Name"] = audioNodeCasted.SelectToken("switch.name.$value").Value<string>();
                    subProps["Switch Value"] = audioNodeCasted.SelectToken("switch.value.$value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
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

                NodeProps subProps = new();

                if (nodeType2 == "questPlayEnv_SetWeather")
                {
                    subProps["Blend Time"] = envManagerNodeCasted.SelectToken("blendTime").Value<string>();
                    subProps["Priority"] = envManagerNodeCasted.SelectToken("priority").Value<string>();
                    subProps["Reset"] = envManagerNodeCasted.SelectToken("reset").Value<string>() == "1" ? "True" : "False";
                    subProps["Source"] = envManagerNodeCasted.SelectToken("source.$value").Value<string>();
                    subProps["Weather ID"] = envManagerNodeCasted.SelectToken("weatherID.$value").Value<string>();
                }
                if (nodeType2 == "questPlayEnv_NodeType")
                {
                    subProps["Blend Time"] = envManagerNodeCasted.SelectToken("params.blendTime").Value<string>();
                    subProps["Enable"] = envManagerNodeCasted.SelectToken("params.enable").Value<string>() == "1" ? "True" : "False";
                    subProps["Env Params"] = envManagerNodeCasted.SelectToken("params.envParams.DepotPath.$value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questRenderFxManagerNodeDefinition")
            {
                var renderFxManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = renderFxManagerNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questSetFadeInOut_NodeType")
                {
                    subProps["Duration"] = renderFxManagerNodeCasted.SelectToken("duration").Value<string>();
                    subProps["Fade Color"] = ParseColor(renderFxManagerNodeCasted.SelectToken("fadeColor"));
                    subProps["Fade In"] = renderFxManagerNodeCasted.SelectToken("fadeIn").Value<string>() == "1" ? "True" : "False";
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questUIManagerNodeDefinition")
            {
                var uiManagerNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = uiManagerNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questSetHUDEntryForcedVisibility_NodeType")
                {
                    var p = uiManagerNodeCasted.SelectToken("hudEntryName");
                    for (int i = 0; i < p.Count(); i++)
                    {
                        subProps["Hud Entry Name #" + i] = p[i].SelectToken("$value").Value<string>();
                    }

                    subProps["Hud Visibility Preset"] = uiManagerNodeCasted.SelectToken("hudVisibilityPreset.$value").Value<string>();
                    subProps["Skip Animation"] = uiManagerNodeCasted.SelectToken("skipAnimation").Value<string>() == "1" ? "True" : "False";
                    subProps["Use Preset"] = uiManagerNodeCasted.SelectToken("usePreset").Value<string>() == "1" ? "True" : "False";
                    subProps["Visibility"] = uiManagerNodeCasted.SelectToken("visibility").Value<string>();
                }
                if (nodeType2 == "questSwitchNameplate_NodeType")
                {
                    subProps["Alternative Name"] = uiManagerNodeCasted.SelectToken("alternativeName").Value<string>() == "1" ? "True" : "False";
                    subProps["Enable"] = uiManagerNodeCasted.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                    subProps["Is Player"] = uiManagerNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    subProps["Puppet Ref"] = ParseGameEntityReference(uiManagerNodeCasted.SelectToken("puppetRef"));
                }
                if (nodeType2 == "questWarningMessage_NodeType")
                {
                    subProps["Duration"] = uiManagerNodeCasted.SelectToken("duration").Value<string>();
                    subProps["Instant"] = uiManagerNodeCasted.SelectToken("instant").Value<string>() == "1" ? "True" : "False";
                    subProps["Localized Message"] = uiManagerNodeCasted.SelectToken("localizedMessage.value").Value<string>();
                    subProps["Message"] = uiManagerNodeCasted.SelectToken("message").Value<string>();
                    subProps["Show"] = uiManagerNodeCasted.SelectToken("show").Value<string>() == "1" ? "True" : "False";
                    subProps["Message Type"] = uiManagerNodeCasted.SelectToken("type").Value<string>();
                }
                if (nodeType2 == "questProgressBar_NodeType")
                {
                    subProps["Bottom Text"] = uiManagerNodeCasted.SelectToken("bottomText.value").Value<string>();
                    subProps["Duration"] = uiManagerNodeCasted.SelectToken("duration").Value<string>();
                    subProps["Show"] = uiManagerNodeCasted.SelectToken("show").Value<string>() == "1" ? "True" : "False";
                    subProps["Text"] = uiManagerNodeCasted.SelectToken("text.value").Value<string>();
                    subProps["Progress Bar Type"] = uiManagerNodeCasted.SelectToken("type").Value<string>();
                }
                if (nodeType2 == "questTutorial_NodeType")
                {
                    var tutorialCasted = uiManagerNodeCasted.SelectToken("subtype.Data");

                    string nodeType3 = tutorialCasted.SelectToken("$type").Value<string>();

                    NodeProps subSubProps = new();

                    if (nodeType3 == "questShowPopup_NodeSubType")
                    {
                        subSubProps["Close At Input"] = tutorialCasted.SelectToken("closeAtInput").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Close Current Popup"] = tutorialCasted.SelectToken("closeCurrentPopup").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Hide In Menu"] = tutorialCasted.SelectToken("hideInMenu").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Ignore Disabled Tutorials"] = tutorialCasted.SelectToken("ignoreDisabledTutorials").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Lock Player Movement"] = tutorialCasted.SelectToken("lockPlayerMovement").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Margin"] = ParseMargin(tutorialCasted.SelectToken("margin"));
                        subSubProps["Open"] = tutorialCasted.SelectToken("open").Value<string>() == "1" ? "True" : "False";
                        subSubProps.AddRange(ParseJournalPath(tutorialCasted.SelectToken("path")));
                        subSubProps["Pause Game"] = tutorialCasted.SelectToken("pauseGame").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Position"] = tutorialCasted.SelectToken("position").Value<string>();
                        subSubProps["Screen Mode"] = tutorialCasted.SelectToken("screenMode").Value<string>();
                        subSubProps["Video"] = tutorialCasted.SelectToken("video.DepotPath.$value").Value<string>();
                        subSubProps["Video Type"] = tutorialCasted.SelectToken("videoType").Value<string>();
                    }
                    if (nodeType3 == "questShowBracket_NodeSubType")
                    {
                        subSubProps["Anchor"] = tutorialCasted.SelectToken("anchor").Value<string>();
                        subSubProps["Bracket ID"] = tutorialCasted.SelectToken("bracketID").Value<string>();
                        subSubProps["Bracket Type"] = tutorialCasted.SelectToken("bracketType").Value<string>();
                        subSubProps["Ignore Disabled Tutorials"] = tutorialCasted.SelectToken("ignoreDisabledTutorials").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Offset"] = tutorialCasted.SelectToken("offset").Value<string>();
                        subSubProps["Size"] = tutorialCasted.SelectToken("size").Value<string>();
                        subSubProps["Visible"] = tutorialCasted.SelectToken("visible").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Visible On UI Layer"] = tutorialCasted.SelectToken("visibleOnUILayer").Value<string>();
                    }
                    if (nodeType3 == "questShowHighlight_NodeSubType")
                    {
                        subSubProps["Enable"] = tutorialCasted.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Entity Reference"] = ParseGameEntityReference(tutorialCasted.SelectToken("entityReference"));
                    }
                    if (nodeType3 == "questShowOverlay_NodeSubType")
                    {
                        subSubProps["Hide On Input"] = tutorialCasted.SelectToken("hideOnInput").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Library Item Name"] = tutorialCasted.SelectToken("libraryItemName").Value<string>();
                        subSubProps["Lock Player Movement"] = tutorialCasted.SelectToken("lockPlayerMovement").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Overlay Library"] = tutorialCasted.SelectToken("overlayLibrary.DepotPath.$value").Value<string>();
                        subSubProps["Pause Game"] = tutorialCasted.SelectToken("pauseGame").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Visible"] = tutorialCasted.SelectToken("visible").Value<string>() == "1" ? "True" : "False";
                    }

                    details["Type", nodeType3] = subSubProps;
                }

                details["Type", nodeType2] = subProps;
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

                NodeProps subProps = new();

                if (nodeType2 == "questShowWorldNode_NodeType")
                {
                    subProps["Component Name"] = worldDataManagerCasted.SelectToken("componentName.$value").Value<string>();
                    subProps["Is Player"] = worldDataManagerCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    subProps["Object Ref"] = worldDataManagerCasted.SelectToken("objectRef.$value").Value<string>();
                    subProps["Show"] = worldDataManagerCasted.SelectToken("show").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questTogglePrefabVariant_NodeType")
                {
                    var paramsArr = worldDataManagerCasted.SelectToken("params");
                    //details["Params Count"] = paramsArr?.Count.ToString()!;

                    int counter = 1;
                    foreach (var re in paramsArr)
                    {
                        subProps["#" + counter + " Prefab Node Ref"] = re.SelectToken("prefabNodeRef.$value").Value<string>();

                        var variantStates = re.SelectToken("variantStates");
                        //details["#" + counter + " Variant States Count"] = variantStates?.Count.ToString()!;

                        int counter2 = 1;
                        foreach (var vs in variantStates)
                        {
                            subProps["#" + counter + " Variant State #" + counter2 + " Name"] = vs.SelectToken("name.$value").Value<string>();
                            subProps["#" + counter + " Variant State #" + counter2 + " Show"] = vs.SelectToken("show").Value<string>() == "1" ? "True" : "False";

                            counter2++;
                        }

                        counter++;
                    }
                }
                if (nodeType2 == "questPrefetchStreaming_NodeTypeV2")
                {
                    subProps["Force Enable"] = worldDataManagerCasted.SelectToken("forceEnable").Value<string>() == "1" ? "True" : "False";
                    subProps["Max Distance"] = worldDataManagerCasted.SelectToken("maxDistance").Value<string>();
                    subProps["Prefetch Position Ref"] = worldDataManagerCasted.SelectToken("prefetchPositionRef.$value").Value<string>();
                    subProps["Use Streaming Occlusion"] = worldDataManagerCasted.SelectToken("useStreamingOcclusion").Value<string>() == "1" ? "True" : "False";
                }

                details["Type", nodeType2] = subProps;
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

                    NodeProps subProps = new();

                    if (nodeType2 == "questScene_NodeType")
                    {
                        subProps["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        subProps["#" + counter + " Entity Reference"] = ParseGameEntityReference(actionCasted.SelectToken("entityReference"));
                    }
                    if (nodeType2 == "questCommunityTemplate_NodeType")
                    {
                        subProps["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        subProps["#" + counter + " Community Entry Name"] = actionCasted.SelectToken("communityEntryName.$value").Value<string>();
                        subProps["#" + counter + " Community Entry Phase Name"] = actionCasted.SelectToken("communityEntryPhaseName.$value").Value<string>();
                        subProps["#" + counter + " Spawner Reference"] = actionCasted.SelectToken("spawnerReference.$value").Value<string>();
                    }
                    if (nodeType2 == "questSpawnSet_NodeType")
                    {
                        subProps["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        subProps["#" + counter + " Entry Name"] = actionCasted.SelectToken("entryName.$value").Value<string>();
                        subProps["#" + counter + " Phase Name"] = actionCasted.SelectToken("phaseName.$value").Value<string>();
                        subProps["#" + counter + " Reference"] = actionCasted.SelectToken("reference.$value").Value<string>();
                    }
                    if (nodeType2 == "questSpawner_NodeType")
                    {
                        subProps["#" + counter + " Action"] = actionCasted.SelectToken("action").Value<string>();
                        subProps["#" + counter + " Spawner Reference"] = actionCasted.SelectToken("spawnerReference.$value").Value<string>();
                    }

                    details["Type", nodeType2] = subProps;

                    counter++;
                }
            }
            else if (nodeType == "questGameManagerNodeDefinition")
            {
                var gameManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = gameManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questGameplayRestrictions_NodeType")
                {
                    subProps["Action"] = gameManagerCasted.SelectToken("action").Value<string>();

                    var restr = gameManagerCasted.SelectToken("restrictionIDs");
                    //details["Restrictions"] = restr?.Count.ToString()!;

                    int counter = 1;
                    foreach (var re in restr)
                    {
                        subProps["#" + counter] = re.SelectToken("$value").Value<string>();

                        counter++;
                    }
                }
                if (nodeType2 == "questRumble_NodeType")
                {
                    subProps["Is Player"] = gameManagerCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    subProps["Object Ref"] = ParseGameEntityReference(gameManagerCasted.SelectToken("objectRef"));
                    subProps["Rumble Event"] = gameManagerCasted.SelectToken("rumbleEvent.$value").Value<string>();
                }
                if (nodeType2 == "questContentTokenManager_NodeType")
                {
                    var contentTokenManagerNodeCasted = gameManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = contentTokenManagerNodeCasted.SelectToken("$type").Value<string>();

                    NodeProps subSubProps = new();

                    if (nodeType3 == "questBlockTokenActivation_NodeSubType")
                    {
                        subSubProps["Action"] = contentTokenManagerNodeCasted.SelectToken("action").Value<string>();
                        subSubProps["Reset Token Spawn Timer"] = contentTokenManagerNodeCasted.SelectToken("resetTokenSpawnTimer").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Source"] = contentTokenManagerNodeCasted.SelectToken("source.$value").Value<string>();
                    }

                    details["Type", nodeType3] = subSubProps;
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questVoicesetManagerNodeDefinition")
            {
                var voicesetManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = voicesetManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questPlayVoiceset_NodeType")
                {
                    var paramsArr = voicesetManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        subProps["#" + counter + " Is Player"] = param.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Override Visual Style"] = param.SelectToken("overrideVisualStyle").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Override Voiceover Expression"] = param.SelectToken("overrideVoiceoverExpression").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Overriding Visual Style"] = param.SelectToken("overridingVisualStyle").Value<string>();
                        subProps["#" + counter + " Overriding Voiceover Context"] = param.SelectToken("overridingVoiceoverContext").Value<string>();
                        subProps["#" + counter + " Overriding Voiceover Expression"] = param.SelectToken("overridingVoiceoverExpression").Value<string>();
                        subProps["#" + counter + " Play Only Grunt"] = param.SelectToken("playOnlyGrunt").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Puppet Ref"] = ParseGameEntityReference(param.SelectToken("puppetRef"));
                        subProps["#" + counter + " Use Voiceset System"] = param.SelectToken("useVoicesetSystem").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Voiceset Name"] = param.SelectToken("voicesetName.$value").Value<string>();

                        counter++;
                    }
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questInteractiveObjectManagerNodeDefinition")
            {
                var interactiveObjectManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = interactiveObjectManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

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
                        subProps["#" + counter + " Action Properties"] = actionProperties;

                        subProps["#" + counter + " Device Action"] = paramCasted.SelectToken("deviceAction.$value").Value<string>();
                        subProps["#" + counter + " Device Controller Class"] = paramCasted.SelectToken("deviceControllerClass.$value").Value<string>();
                        subProps["#" + counter + " Entity Ref"] = ParseGameEntityReference(paramCasted.SelectToken("entityRef"));
                        subProps["#" + counter + " Object Ref"] = paramCasted.SelectToken("objectRef.$value").Value<string>();
                        subProps["#" + counter + " Slot Name"] = paramCasted.SelectToken("slotName.$value").Value<string>();

                        counter++;
                    }
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questCharacterManagerNodeDefinition")
            {
                var characterManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = characterManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questCharacterManagerParameters_NodeType")
                {
                    var charParamsNodeCasted = characterManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = charParamsNodeCasted.SelectToken("$type").Value<string>();

                    NodeProps subSubProps = new();

                    if (nodeType3 == "questCharacterManagerParameters_SetStatusEffect")
                    {
                        subSubProps["Is Player"] = charParamsNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Is Player Status Effect Source"] = charParamsNodeCasted.SelectToken("isPlayerStatusEffectSource").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Puppet Ref"] = ParseGameEntityReference(charParamsNodeCasted.SelectToken("puppetRef"));
                        //setStatusEffectNodeCasted?.RecordSelector
                        subSubProps["Set"] = charParamsNodeCasted.SelectToken("set").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Status Effect ID"] = charParamsNodeCasted.SelectToken("statusEffectID.$value").Value<string>();
                        subSubProps["Status Effect Source Object"] = ParseGameEntityReference(charParamsNodeCasted.SelectToken("statusEffectSourceObject"));
                    }

                    details["Type", nodeType3] = subSubProps;
                }
                if (nodeType2 == "questCharacterManagerVisuals_NodeType")
                {
                    var charVisualsNodeCasted = characterManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = charVisualsNodeCasted.SelectToken("$type").Value<string>();

                    NodeProps subSubProps = new();

                    if (nodeType3 == "questCharacterManagerVisuals_GenitalsManager")
                    {
                        subSubProps["Body Group Name"] = charVisualsNodeCasted.SelectToken("bodyGroupName.$value").Value<string>();
                        subSubProps["Enable"] = charVisualsNodeCasted.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Is Player"] = charVisualsNodeCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Puppet Ref"] = ParseGameEntityReference(charVisualsNodeCasted.SelectToken("puppetRef"));
                    }
                    if (nodeType3 == "questCharacterManagerVisuals_ChangeEntityAppearance")
                    {
                        var appearancesArr = charVisualsNodeCasted.SelectToken("appearanceEntries");

                        int counter = 1;
                        foreach (var appearance in appearancesArr)
                        {
                            subSubProps["#" + counter + " Appearance Name"] = appearance.SelectToken("appearanceName.$value").Value<string>();
                            subSubProps["#" + counter + " Is Player"] = appearance.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                            subSubProps["#" + counter + " Puppet Ref"] = ParseGameEntityReference(appearance.SelectToken("puppetRef"));

                            counter++;
                        }
                    }

                    details["Type", nodeType3] = subSubProps;
                }
                if (nodeType2 == "questCharacterManagerCombat_NodeType")
                {
                    var charCombatNodeCasted = characterManagerCasted.SelectToken("subtype.Data");

                    string nodeType3 = charCombatNodeCasted.SelectToken("$type").Value<string>();

                    NodeProps subSubProps = new();

                    if (nodeType3 == "questCharacterManagerCombat_EquipWeapon")
                    {
                        subSubProps["Equip"] = charCombatNodeCasted.SelectToken("equip").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Equip Last Weapon"] = charCombatNodeCasted.SelectToken("equipLastWeapon").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Force First Equip"] = charCombatNodeCasted.SelectToken("forceFirstEquip").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Ignore State Machine"] = charCombatNodeCasted.SelectToken("ignoreStateMachine").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Instant"] = charCombatNodeCasted.SelectToken("instant").Value<string>() == "1" ? "True" : "False";
                        subSubProps["Slot ID"] = charCombatNodeCasted.SelectToken("slotID.$value").Value<string>();
                        subSubProps["Weapon ID"] = charCombatNodeCasted.SelectToken("weaponID.$value").Value<string>();
                    }

                    details["Type", nodeType3] = subSubProps;
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questItemManagerNodeDefinition")
            {
                var itemManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = itemManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questAddRemoveItem_NodeType")
                {
                    var paramsArr = itemManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        var paramCasted = param.SelectToken("Data");

                        subProps["#" + counter + " Entity Ref"] = GetNameFromUniversalRef(paramCasted.SelectToken("entityRef"));
                        subProps["#" + counter + " Flag Item Added Callback As Silent"] = paramCasted.SelectToken("flagItemAddedCallbackAsSilent").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Is Player"] = paramCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Item ID"] = paramCasted.SelectToken("itemID.$value").Value<string>();

                        var itemToIgnore = "";
                        foreach (var p in paramCasted.SelectToken("itemIDsToIgnoreOnRemove"))
                        {
                            itemToIgnore += (itemToIgnore != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                        }
                        subProps["#" + counter + " Item IDs To Ignore On Remove"] = itemToIgnore;

                        subProps["#" + counter + " Node Type"] = paramCasted.SelectToken("nodeType").Value<string>();
                        subProps["#" + counter + " Object Ref"] = ParseGameEntityReference(paramCasted.SelectToken("objectRef"));
                        subProps["#" + counter + " Quantity"] = paramCasted.SelectToken("quantity").Value<string>();
                        subProps["#" + counter + " Remove All Quantity"] = paramCasted.SelectToken("removeAllQuantity").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Send Notification"] = paramCasted.SelectToken("sendNotification").Value<string>() == "1" ? "True" : "False";

                        var tagsToIgnore = "";
                        foreach (var p in paramCasted.SelectToken("tagsToIgnoreOnRemove"))
                        {
                            tagsToIgnore += (tagsToIgnore != "" ? ", " : "") + p.SelectToken("$value").Value<string>();
                        }
                        subProps["#" + counter + " Tags To Ignore On Remove"] = tagsToIgnore;

                        subProps["#" + counter + " Tag To Remove"] = paramCasted.SelectToken("tagToRemove.$value").Value<string>();

                        counter++;
                    }
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questCrowdManagerNodeDefinition")
            {
                var crowdManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = crowdManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questCrowdManagerNodeType_ControlCrowd")
                {
                    subProps["Action"] = crowdManagerCasted.SelectToken("action").Value<string>();
                    subProps["Debug Source"] = crowdManagerCasted.SelectToken("debugSource.$value").Value<string>();
                    subProps["Distant Crowd Only"] = crowdManagerCasted.SelectToken("distantCrowdOnly").Value<string>() == "1" ? "True" : "False";
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questFXManagerNodeDefinition")
            {
                var fxManagerCasted = node.SelectToken("type.Data");

                string nodeType2 = fxManagerCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questPlayFX_NodeType")
                {
                    var paramsArr = fxManagerCasted.SelectToken("params");
                    //details["Actions"] = actions.Count.ToString();

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        subProps["#" + counter + " Effect Instance Name"] = param.SelectToken("effectInstanceName.$value").Value<string>();
                        subProps["#" + counter + " Effect Name"] = param.SelectToken("effectName.$value").Value<string>();
                        subProps["#" + counter + " Is Player"] = param.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Object Ref"] = ParseGameEntityReference(param.SelectToken("objectRef"));
                        subProps["#" + counter + " Play"] = param.SelectToken("play").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Save"] = param.SelectToken("save").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Sequence Shift"] = param.SelectToken("sequenceShift").Value<string>();

                        counter++;
                    }
                }

                details["Type", nodeType2] = subProps;
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

                NodeProps subProps = new();

                if (nodeType2 == "questEntityManagerToggleMirrorsArea_NodeType")
                {
                    subProps["Is In Mirrors Area"] = entityManagerCasted.SelectToken("isInMirrorsArea").Value<string>() == "1" ? "True" : "False";
                    subProps["Object Ref"] = ParseGameEntityReference(entityManagerCasted.SelectToken("objectRef"));
                }
                if (nodeType2 == "questEntityManagerChangeAppearance_NodeType")
                {
                    subProps["Appearance Name"] = entityManagerCasted.SelectToken("appearanceName.$value").Value<string>();
                    subProps["Entity Ref"] = ParseGameEntityReference(entityManagerCasted.SelectToken("entityRef"));
                    subProps["Prefetch Only"] = entityManagerCasted.SelectToken("prefetchOnly").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questEntityManagerToggleComponent_NodeType")
                {
                    var paramsArr = entityManagerCasted.SelectToken("params");

                    int counter = 1;
                    foreach (var param in paramsArr)
                    {
                        subProps["#" + counter + " Component Name"] = param.SelectToken("componentName.$value").Value<string>();
                        subProps["#" + counter + " Enable"] = param.SelectToken("enable").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Is Player"] = param.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                        subProps["#" + counter + " Object Ref"] = ParseGameEntityReference(param.SelectToken("objectRef"));

                        counter++;
                    }
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questMovePuppetNodeDefinition")
            {
                details["Entity Reference"] = ParseGameEntityReference(node.SelectToken("entityReference"));
                details["Move Type"] = node.SelectToken("moveType.$value").Value<string>();

                string nodeType2 = node.SelectToken("nodeParams.Data.$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questMoveOnSplineParams")
                {
                    var splineParamsCasted = node.SelectToken("nodeParams.Data");

                    subProps["Spline Node Ref"] = splineParamsCasted.SelectToken("splineNodeRef.$value").Value<string>();
                }

                details["Type", nodeType2] = subProps;
            }
            else if (nodeType == "questVehicleNodeDefinition")
            {
                var vehicleNodeCasted = node.SelectToken("type.Data");

                string nodeType2 = vehicleNodeCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questMoveOnSpline_NodeType")
                {
                    subProps["Arrive With Pivot"] = vehicleNodeCasted.SelectToken("arriveWithPivot").Value<string>() == "1" ? "True" : "False";
                    subProps["Audio Curves"] = vehicleNodeCasted.SelectToken("audioCurves.DepotPath.$value").Value<string>();
                    subProps["Blend Time"] = vehicleNodeCasted.SelectToken("blendTime").Value<string>();
                    subProps["Blend Type"] = vehicleNodeCasted.SelectToken("blendType").Value<string>();
                    subProps["Overrides"] = vehicleNodeCasted.SelectToken("overrides") != null ? "Has overrides" : "";
                    subProps["Reverse Gear"] = vehicleNodeCasted.SelectToken("reverseGear").Value<string>() == "1" ? "True" : "False";
                    subProps["Scene Blend In Distance"] = vehicleNodeCasted.SelectToken("sceneBlendInDistance").Value<string>();
                    subProps["Scene Blend Out Distance"] = vehicleNodeCasted.SelectToken("sceneBlendOutDistance").Value<string>();
                    subProps["Spline Ref"] = vehicleNodeCasted.SelectToken("splineRef.$value").Value<string>();
                    subProps["Start From"] = vehicleNodeCasted.SelectToken("startFrom").Value<string>();
                    subProps["Traffic Deletion Mode"] = vehicleNodeCasted.SelectToken("trafficDeletionMode").Value<string>();
                    subProps["Vehicle Ref"] = ParseGameEntityReference(vehicleNodeCasted.SelectToken("vehicleRef"));
                }
                if (nodeType2 == "questTeleport_NodeType")
                {
                    subProps["Entity Reference"] = ParseGameEntityReference(vehicleNodeCasted.SelectToken("entityReference"));
                    subProps["Destination Offset"] = ParseVector(vehicleNodeCasted.SelectToken("params.destinationOffset"));
                    subProps["Destination Ref"] = GetNameFromUniversalRef(vehicleNodeCasted.SelectToken("params.destinationRef.Data"));
                }

                details["Type", nodeType2] = subProps;
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
            else if (nodeType == "questEquipItemNodeDefinition")
            {
                details["Entity Reference"] = ParseGameEntityReference(node.SelectToken("entityReference.Data.entityReference"));

                var paramsCasted = node.SelectToken("params.Data");

                string nodeType2 = paramsCasted.SelectToken("$type").Value<string>();

                NodeProps subProps = new();

                if (nodeType2 == "questEquipItemParams")
                {
                    subProps["By Item"] = paramsCasted.SelectToken("byItem").Value<string>() == "1" ? "True" : "False";
                    subProps["Equip Duration Override"] = paramsCasted.SelectToken("equipDurationOverride").Value<string>();
                    subProps["Equip Last Weapon"] = paramsCasted.SelectToken("equipLastWeapon").Value<string>() == "1" ? "True" : "False";
                    subProps["Equip Types"] = paramsCasted.SelectToken("equipTypes").Value<string>();
                    subProps["Fail If Item Not Found"] = paramsCasted.SelectToken("failIfItemNotFound").Value<string>() == "1" ? "True" : "False";
                    subProps["Force First Equip"] = paramsCasted.SelectToken("forceFirstEquip").Value<string>() == "1" ? "True" : "False";
                    subProps["Ignore State Machine"] = paramsCasted.SelectToken("ignoreStateMachine").Value<string>() == "1" ? "True" : "False";
                    subProps["Instant"] = paramsCasted.SelectToken("instant").Value<string>() == "1" ? "True" : "False";
                    subProps["Is Player"] = paramsCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    subProps["Item Id"] = paramsCasted.SelectToken("itemId.$value").Value<string>();
                    subProps["Slot Id"] = paramsCasted.SelectToken("slotId.$value").Value<string>();
                    subProps["Type"] = paramsCasted.SelectToken("type").Value<string>();
                    subProps["Unequip Duration Override"] = paramsCasted.SelectToken("unequipDurationOverride").Value<string>();
                    subProps["Unequip Types"] = paramsCasted.SelectToken("unequipTypes").Value<string>();
                }

                details["Params", nodeType2] = subProps;
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

                    var dur = eventClass.SelectToken("Data.duration")?.Value<string>();
                    if (dur != null)
                        subProps["Duration"] = dur;

                    if (evName == "scneventsSocket")
                    {
                        evName += " - Name: " + eventClass.SelectToken("Data.osockStamp.name").Value<string>() + ", Ordinal: " + eventClass.SelectToken("Data.osockStamp.ordinal").Value<string>();
                    }
                    else if (evName == "scnPlaySkAnimEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);
                        subProps["Origin Marker Node Ref"] = eventClass.SelectToken("Data.rootMotionData.originMarker.nodeRef.$value").Value<string>();
                        subProps["Actor Component"] = eventClass.SelectToken("Data.actorComponent.$value").Value<string>();

                        var animType = eventClass.SelectToken("Data.animName.Data.type").Value<string>();

                        if (animType == "direct")
                            subProps["Anim"] = eventClass?.SelectToken("Data.animName.Data.unk1[0].$value")?.Value<string>();

                        if (animType == "reference")
                            subProps.AddRange(GetRIDAnimDetails(eventClass?.SelectToken("Data.animName.Data.unk2[0]")?.Value<string>(), "", "", scnSceneResource));

                        if (animType == "container")
                            subProps.AddRange(GetRIDAnimDetails("", eventClass?.SelectToken("Data.animName.Data.unk2[0]")?.Value<string>(), "", scnSceneResource));
                    }
                    else if (evName == "scnPlayRidAnimEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);
                        subProps["Origin Marker Node Ref"] = eventClass.SelectToken("Data.animOriginMarker.nodeRef.$value").Value<string>();
                        subProps["Actor Component"] = eventClass.SelectToken("Data.actorComponent.$value").Value<string>();
                        subProps.AddRange(GetRIDAnimDetails(eventClass?.SelectToken("Data.animResRefId.id")?.Value<string>(), "", "", scnSceneResource));
                    }
                    else if (evName == "scneventsPlayRidCameraAnimEvent")
                    {
                        subProps["Camera Ref"] = eventClass.SelectToken("Data.cameraRef.$value").Value<string>();
                        subProps["Origin Marker Node Ref"] = eventClass.SelectToken("Data.animOriginMarker.nodeRef.$value").Value<string>();
                        subProps.AddRange(GetRIDAnimDetails("", "", eventClass?.SelectToken("Data.animSRRefId.id")?.Value<string>(), scnSceneResource));
                    }
                    else if (evName == "scnAudioEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);
                        subProps["Event"] = eventClass.SelectToken("Data.audioEventName.$value").Value<string>();
                    }
                    else if (evName == "scnDialogLineEvent")
                    {
                        //subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);
                        //subProps["Screenplay Line Id"] = GetScreenplayItem(eventClass.SelectToken("Data.screenplayLineId.id").Value<string>(), scnSceneResource);
                        subProps.AddRange(GetScreenplayItem(eventClass.SelectToken("Data.screenplayLineId.id").Value<string>(), scnSceneResource));
                    }
                    else if (evName == "scnLookAtEvent")
                    {
                        subProps["Is Start"] = eventClass.SelectToken("Data.basicData.basic.isStart").Value<string>() == "1" ? "True" : "False";
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.basicData.basic.performerId.id").Value<string>(), scnSceneResource);
                        subProps["Static Target"] = ParseVector(eventClass.SelectToken("Data.basicData.basic.staticTarget"));
                        subProps["Target Performer"] = GetPerformer(eventClass.SelectToken("Data.basicData.basic.targetPerformerId.id").Value<string>(), scnSceneResource);
                    }
                    else if (evName == "scneventsAttachPropToPerformer")
                    {
                        subProps["To Performer"] = GetPerformer(eventClass.SelectToken("Data.performerId.id").Value<string>(), scnSceneResource);
                        subProps["Prop"] = GetProp(eventClass.SelectToken("Data.propId.id").Value<string>(), scnSceneResource);
                    }
                    else if (evName == "scneventsAttachPropToNode")
                    {
                        subProps["Prop"] = GetProp(eventClass.SelectToken("Data.propId.id").Value<string>(), scnSceneResource);
                        subProps["Node Ref"] = eventClass.SelectToken("Data.nodeRef.$value").Value<string>();
                        subProps["Offset Pos"] = ParseVector(eventClass.SelectToken("Data.customOffsetPos"));
                        subProps["Offset Rot"] = ParseQuaternion(eventClass.SelectToken("Data.customOffsetRot"));
                    }
                    else if (evName == "scneventsAttachPropToWorld")
                    {
                        subProps["Prop"] = GetProp(eventClass.SelectToken("Data.propId.id").Value<string>(), scnSceneResource);
                        subProps["Offset Pos"] = ParseVector(eventClass.SelectToken("Data.customOffsetPos"));
                        subProps["Offset Rot"] = ParseQuaternion(eventClass.SelectToken("Data.customOffsetRot"));
                    }
                    else if (evName == "scnChangeIdleAnimEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performer.id").Value<string>(), scnSceneResource);
                        subProps["Baked Facial Female"] = eventClass.SelectToken("Data.bakedFacialTransition.facialKey_Female.$value").Value<string>();
                        subProps["Baked Facial Male"] = eventClass.SelectToken("Data.bakedFacialTransition.facialKey_Male.$value").Value<string>();
                        subProps["Baked To Idle Female"] = eventClass.SelectToken("Data.bakedFacialTransition.toIdleFemale.$value").Value<string>();
                        subProps["Baked To Idle Male"] = eventClass.SelectToken("Data.bakedFacialTransition.toIdleMale.$value").Value<string>();
                    }
                    else if (evName == "scnPoseCorrectionEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.performerId.id").Value<string>(), scnSceneResource);
                    }
                    else if (evName == "scnIKEvent")
                    {
                        subProps["Performer"] = GetPerformer(eventClass.SelectToken("Data.ikData.basic.performerId.id").Value<string>(), scnSceneResource);
                    }
                    else if (evName == "scneventsCameraParamsEvent")
                    {
                        subProps["Override Dof"] = eventClass.SelectToken("Data.cameraOverrideSettings.overrideDof").Value<string>() == "1" ? "True" : "False";
                        subProps["Override Fov"] = eventClass.SelectToken("Data.cameraOverrideSettings.overrideFov").Value<string>() == "1" ? "True" : "False";
                        subProps["Reset Dof"] = eventClass.SelectToken("Data.cameraOverrideSettings.resetDof").Value<string>() == "1" ? "True" : "False";
                        subProps["Reset Fov"] = eventClass.SelectToken("Data.cameraOverrideSettings.resetFov").Value<string>() == "1" ? "True" : "False";
                    }

                    var timeBegin = int.Parse(eventClass.SelectToken("Data.startTime").Value<string>());
                    details["#" + counter.ToString() + " " + timeBegin.ToString() + "ms" + (dur != null ? (" > " + (timeBegin + int.Parse(dur)).ToString() + "ms") : "") + " ET:" + eventClass.SelectToken("Data.executionTagFlags").Value<string>(), evName] = subProps;
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
                    details["Actor"] = GetActor(node.SelectToken("ataParams.actorId.id").Value<string>(), scnSceneResource);
                }
                if (mode == "attachToGameObject")
                {
                    details["Node Ref"] = node.SelectToken("atgoParams.nodeRef.$value").Value<string>();
                }
                if (mode == "attachToProp")
                {
                    details["Prop"] = GetProp(node.SelectToken("atpParams.propId.id").Value<string>(), scnSceneResource);
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

                    var type = option.SelectToken("type.properties").Value<int>();
                    var typesStr = "";
                    //foreach (gameinteractionsChoiceType tp in Enum.GetValues<gameinteractionsChoiceType>())
                    var enumVals = Enum.GetValues<gameinteractionsChoiceType>();
                    for (int i = 0; i < enumVals.Length; i++)
                    {
                        if (isBitNActive(type, i)) typesStr += (typesStr != "" ? ", " : "") + Enum.GetName(enumVals[i]);
                    }
                    subProps["Type Properties"] = type.ToString() + " (" + typesStr + ")";

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

            NodeProps subProps = new();

            if (nodeType == "questTimeCondition")
            {
                var condTimeCasted = node.SelectToken("type.Data");

                string nodeType2 = condTimeCasted.SelectToken("$type").Value<string>();

                //string ConditionTimeTypeName = "Time condition";
                string ConditionTimeValTypeName = "Time";

                NodeProps subSubProps = new();

                if (nodeType2 == "questRealtimeDelay_ConditionType")
                {
                    //details[ConditionTimeTypeName] = GetNameFromClass(nodeRealtimeCondCasted);
                    subSubProps[ConditionTimeValTypeName] =
                        FormatNumber(condTimeCasted.SelectToken("hours").Value<uint>()) + "h:" +
                        FormatNumber(condTimeCasted.SelectToken("minutes").Value<uint>()) + "m:" +
                        FormatNumber(condTimeCasted.SelectToken("seconds").Value<uint>()) + "s:" +
                        FormatNumber(condTimeCasted.SelectToken("miliseconds").Value<uint>(), true) + "ms";
                }

                if (nodeType2 == "questGameTimeDelay_ConditionType")
                {
                    subSubProps[ConditionTimeValTypeName] =
                        FormatNumber(condTimeCasted.SelectToken("days").Value<uint>()) + "d " +
                        FormatNumber(condTimeCasted.SelectToken("hours").Value<uint>()) + "h:" +
                        FormatNumber(condTimeCasted.SelectToken("minutes").Value<uint>()) + "m:" +
                        FormatNumber(condTimeCasted.SelectToken("seconds").Value<uint>()) + "s";
                }

                if (nodeType2 == "questTickDelay_ConditionType")
                {
                    subSubProps[ConditionTimeValTypeName] = FormatNumber(condTimeCasted.SelectToken("tickCount").Value<uint>()) + " ticks";
                }

                if (nodeType2 == "questTimePeriod_ConditionType")
                {
                    string timeNameFrom = "";
                    string timeNameTo = "";

                    TimeSpan t = TimeSpan.FromSeconds(condTimeCasted.SelectToken("begin.seconds").Value<double>());
                    timeNameFrom = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t.Hours, t.Minutes, t.Seconds);

                    TimeSpan t2 = TimeSpan.FromSeconds(condTimeCasted.SelectToken("end.seconds").Value<double>());
                    timeNameTo = string.Format("{0:D2}h:{1:D2}m:{2:D2}s", t2.Hours, t2.Minutes, t2.Seconds);

                    subSubProps[ConditionTimeValTypeName] =
                        "from " + FormatNumber(condTimeCasted.SelectToken("begin.seconds").Value<uint>()) + " (" + timeNameFrom + ")" +
                        " to " + FormatNumber(condTimeCasted.SelectToken("end.seconds").Value<uint>()) + " (" + timeNameTo + ")";
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questFactsDBCondition")
            {
                var condFactCasted = node.SelectToken("type.Data");

                string nodeType2 = condFactCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questVarComparison_ConditionType")
                {
                    subSubProps["Fact Name"] = condFactCasted.SelectToken("factName").Value<string>();
                    subSubProps["Comparison Type"] = condFactCasted.SelectToken("comparisonType").Value<string>();
                    subSubProps["Value"] = condFactCasted.SelectToken("value").Value<string>();
                }

                if (nodeType2 == "questVarVsVarComparison_ConditionType")
                {
                    subSubProps["Comparison Type"] = condFactCasted.SelectToken("comparisonType").Value<string>();
                    subSubProps["Fact 1 Name"] = condFactCasted.SelectToken("factName1").Value<string>();
                    subSubProps["Fact 2 Name"] = condFactCasted.SelectToken("factName2").Value<string>();
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questLogicalCondition")
            {
                //details["Operation"] = node.SelectToken("operation").Value<string>();

                NodeProps subSubProps = new();

                var conditions = node.SelectToken("conditions");
                for (int i = 0; i < conditions.Count(); i++)
                {
                    subSubProps.AddRange(GetPropertiesForConditions(conditions[i].SelectToken("Data"), "#" + i + " "));
                }

                subProps["Operation", node.SelectToken("operation").Value<string>()] = subSubProps;
            }
            else if (nodeType == "questCharacterCondition")
            {
                var condCharacterCasted = node.SelectToken("type.Data");

                string nodeType2 = condCharacterCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questCharacterBodyType_CondtionType")
                {
                    subSubProps["Gender"] = condCharacterCasted.SelectToken("gender.$value").Value<string>();
                    subSubProps["Object Ref"] = ParseGameEntityReference(condCharacterCasted.SelectToken("objectRef"));
                    subSubProps["Is Player"] = condCharacterCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questCharacterSpawned_ConditionType")
                {
                    subSubProps["Comparison Type"] = condCharacterCasted.SelectToken("comparisonParams.Data.comparisonType").Value<string>();
                    subSubProps["Comparison Count"] = condCharacterCasted.SelectToken("comparisonParams.Data.count").Value<string>();
                    subSubProps["Comparison Entire Community"] = condCharacterCasted.SelectToken("comparisonParams.Data.entireCommunity").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Object Ref"] = ParseGameEntityReference(condCharacterCasted.SelectToken("objectRef"));
                }
                if (nodeType2 == "questCharacterKilled_ConditionType")
                {
                    subSubProps["Comparison Type"] = condCharacterCasted.SelectToken("comparisonParams.Data.comparisonType").Value<string>();
                    subSubProps["Comparison Count"] = condCharacterCasted.SelectToken("comparisonParams.Data.count").Value<string>();
                    subSubProps["Comparison Entire Community"] = condCharacterCasted.SelectToken("comparisonParams.Data.entireCommunity").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Defeated"] = condCharacterCasted.SelectToken("defeated").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Killed"] = condCharacterCasted.SelectToken("killed").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Object Ref"] = ParseGameEntityReference(condCharacterCasted.SelectToken("objectRef"));
                    subSubProps["Source"] = GetNameFromUniversalRef(condCharacterCasted.SelectToken("source"));
                    subSubProps["Unconscious"] = condCharacterCasted.SelectToken("unconscious").Value<string>() == "1" ? "True" : "False";
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questTriggerCondition")
            {
                subProps["Activator Ref"] = ParseGameEntityReference(node.SelectToken("activatorRef"));
                subProps["Is Player Activator"] = node.SelectToken("isPlayerActivator").Value<string>() == "1" ? "True" : "False";
                subProps["Trigger Area Ref"] = node.SelectToken("triggerAreaRef.$value").Value<string>();
                subProps["Trigger Type"] = node.SelectToken("type").Value<string>();
            }
            else if (nodeType == "questSystemCondition")
            {
                var condSystemCasted = node.SelectToken("type.Data");

                string nodeType2 = condSystemCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questCameraFocus_ConditionType")
                {
                    subSubProps["Angle Tolerance"] = condSystemCasted.SelectToken("angleTolerance").Value<string>();
                    subSubProps["Inverted"] = condSystemCasted.SelectToken("inverted").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Object Ref"] = ParseGameEntityReference(condSystemCasted.SelectToken("objectRef"));
                    subSubProps["On Screen Test"] = condSystemCasted.SelectToken("onScreenTest").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Time Interval"] = condSystemCasted.SelectToken("timeInterval").Value<string>();
                    subSubProps["Use Frustrum Check"] = condSystemCasted.SelectToken("useFrustrumCheck").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Zoomed"] = condSystemCasted.SelectToken("zoomed").Value<string>() == "1" ? "True" : "False";
                }
                if (nodeType2 == "questInputAction_ConditionType")
                {
                    subSubProps["Any Input Action"] = condSystemCasted.SelectToken("anyInputAction").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Axis Action"] = condSystemCasted.SelectToken("axisAction").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Check If Button Already Pressed"] = condSystemCasted.SelectToken("checkIfButtonAlreadyPressed").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Input Action"] = condSystemCasted.SelectToken("inputAction.$value").Value<string>();
                    subSubProps["Value Less Than"] = condSystemCasted.SelectToken("valueLessThan").Value<string>();
                    subSubProps["Value More Than"] = condSystemCasted.SelectToken("valueMoreThan").Value<string>();
                }
                if (nodeType2 == "questPrereq_ConditionType")
                {
                    subSubProps["Is Object Player"] = condSystemCasted.SelectToken("isObjectPlayer").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Object Ref"] = ParseGameEntityReference(condSystemCasted.SelectToken("objectRef"));

                    var prereq = condSystemCasted.SelectToken("prereq.Data");
                    var type = prereq.SelectToken("$type").Value<string>();

                    subSubProps["Prereq"] = type;

                    if (type == "DialogueChoiceHubPrereq")
                        subSubProps["Is Choice Hub Active"] = prereq.SelectToken("isChoiceHubActive").Value<string>();

                    if (type == "PlayerControlsDevicePrereq")
                        subSubProps["Inverse"] = prereq.SelectToken("inverse").Value<string>() == "1" ? "True" : "False";
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questDistanceCondition")
            {
                var condDistanceCasted = node.SelectToken("type.Data");

                string nodeType2 = condDistanceCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questDistanceComparison_ConditionType")
                {
                    subSubProps["Comparison Type"] = condDistanceCasted.SelectToken("comparisonType").Value<string>();
                    subSubProps["Entity Ref"] = GetNameFromUniversalRef(condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data"));
                    //details["Entity Ref - Entity Reference"] = GetNameFromUniversalRef(condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data"));
                    //details["Entity Ref - Main Player Object"] = condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data.mainPlayerObject").Value<string>() == "1" ? "True" : "False";
                    //details["Entity Ref - Ref Local Player"] = condDistanceCasted.SelectToken("distanceDefinition1.Data.entityRef.Data.refLocalPlayer").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Node Ref 2"] = ParseGameEntityReference(condDistanceCasted.SelectToken("distanceDefinition1.Data.nodeRef2"));
                    subSubProps["Distance Value"] = condDistanceCasted.SelectToken("distanceDefinition2.Data.distanceValue").Value<string>();
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questSceneCondition")
            {
                var condSceneCasted = node.SelectToken("type.Data");

                string nodeType2 = condSceneCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questSectionNode_ConditionType")
                {
                    subSubProps["Scene File"] = condSceneCasted.SelectToken("sceneFile.DepotPath.$value").Value<string>();
                    subSubProps["Scene Version"] = condSceneCasted.SelectToken("SceneVersion").Value<string>();
                    subSubProps["Section Name"] = condSceneCasted.SelectToken("sectionName.$value").Value<string>();
                    subSubProps["Cond Type"] = condSceneCasted.SelectToken("type").Value<string>();
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questJournalCondition")
            {
                var journalCasted = node.SelectToken("type.Data");

                string nodeType2 = journalCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questJournalEntryState_ConditionType")
                {
                    subSubProps["Inverted"] = journalCasted.SelectToken("inverted").Value<string>() == "1" ? "True" : "False";
                    subSubProps.AddRange(ParseJournalPath(journalCasted.SelectToken("path.Data")));
                    subSubProps["State"] = journalCasted.SelectToken("state").Value<string>();
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }
            else if (nodeType == "questObjectCondition")
            {
                var objectCasted = node.SelectToken("type.Data");

                string nodeType2 = objectCasted.SelectToken("$type").Value<string>();

                NodeProps subSubProps = new();

                if (nodeType2 == "questInventory_ConditionType")
                {
                    subSubProps["Comparison Type"] = objectCasted.SelectToken("comparisonType").Value<string>();
                    subSubProps["Is Player"] = objectCasted.SelectToken("isPlayer").Value<string>() == "1" ? "True" : "False";
                    subSubProps["Item ID"] = objectCasted.SelectToken("itemID.$value").Value<string>();
                    subSubProps["Item Tag"] = objectCasted.SelectToken("itemTag.$value").Value<string>();
                    subSubProps["Object Ref"] = ParseGameEntityReference(objectCasted.SelectToken("objectRef"));
                    subSubProps["Quantity"] = objectCasted.SelectToken("quantity").Value<string>();
                }

                subProps["Subtype", GetNameFromClass(nodeType2)] = subSubProps;
            }

            details[logicalCondIndex + "Type", GetNameFromClass(nodeType)] = subProps;

            return details;
        }

        private static NodeProps GetRIDAnimDetails(string ridAnim, string ridContainer, string ridCameraAnim, JToken scnSceneResource)
        {
            NodeProps details = new();

            if (scnSceneResource != null)
            {
                string getRIDAnimResPath(string ridResID)
                {
                    var path = "";

                    var ridRess = scnSceneResource.SelectToken("ridResources");
                    foreach (var ridResss in ridRess)
                    {
                        if (ridResss.SelectToken("id.id").Value<string>() == ridResID)
                        {
                            path = ridResss.SelectToken("ridResource.DepotPath.$value").Value<string>();
                            path = path.Split('\\')[^1];
                        }
                    }

                    return ridResID + " (" + path + ")";
                }

                string[] getRIDAnimData(string ridAnimID)
                {
                    var ridAnimRes = scnSceneResource.SelectToken("resouresReferences.ridAnimations.[" + ridAnimID + "]");
                    var ridSN = ridAnimRes.SelectToken("animationSN.serialNumber").Value<string>();
                    string ridRes = ridAnimRes.SelectToken("resourceId.id").Value<string>();

                    return [ridSN, getRIDAnimResPath(ridRes)];
                }

                if (ridAnim != "")
                {
                    var d = getRIDAnimData(ridAnim);
                    details["RID Anim Index"] = "#" + ridAnim;
                    details["RID Anim SN"] = d[0];
                    details["RID Res"] = d[1];
                }

                if (ridCameraAnim != "")
                {
                    var ridAnimRes = scnSceneResource.SelectToken("resouresReferences.ridCameraAnimations.[" + ridCameraAnim + "]");
                    var ridSN = ridAnimRes.SelectToken("animationSN.serialNumber").Value<string>();
                    string ridRes = ridAnimRes.SelectToken("resourceId.id").Value<string>();

                    details["RID Camera Anim Index"] = "#" + ridCameraAnim;
                    details["RID Camera Anim SN"] = ridSN;
                    details["RID Res"] = getRIDAnimResPath(ridRes);
                }

                if (ridContainer != "")
                {
                    details["RID Container Index"] = "#" + ridContainer;

                    var ridContainerRes = scnSceneResource.SelectToken("resouresReferences.ridAnimationContainers.[" + ridContainer + "]");

                    int i = 0;
                    NodeProps subDetails = new();

                    var ridContainerResAnims = ridContainerRes.SelectToken("animations");
                    foreach (var an in ridContainerResAnims)
                    {
                        var animID = an.SelectToken("animation.id").Value<string>();
                        var animContext = an.SelectToken("context.genderMask.mask").Value<string>();

                        var d = getRIDAnimData(animID);
                        subDetails["#" + i + " RID Context Gender Mask"] = animContext;
                        subDetails["#" + i + " RID Anim SN"] = d[0];
                        subDetails["#" + i + " RID Res"] = d[1];

                        i++;
                    }

                    details["RID Container", "Animations"] = subDetails;
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

        private static NodeProps GetScreenplayItem(string screenplayItemID, JToken scnSceneResource)
        {
            NodeProps details = new();

            if (scnSceneResource != null)
            {
                foreach (var screenplayStoreLines in scnSceneResource.SelectToken("screenplayStore.lines"))
                {
                    if (screenplayStoreLines.SelectToken("itemId.id").Value<string>() == screenplayItemID)
                    {
                        details["Item Id"] = screenplayStoreLines.SelectToken("itemId.id").Value<string>();
                        details["Locstring Id"] = screenplayStoreLines.SelectToken("locstringId.ruid").Value<string>();
                        details["Addressee"] = GetActor(screenplayStoreLines.SelectToken("addressee.id").Value<string>(), scnSceneResource);
                        details["Speaker"] = GetActor(screenplayStoreLines.SelectToken("speaker.id").Value<string>(), scnSceneResource);
                        break;
                    }
                }
            }

            return details;
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

        private static string GetProp(string propID, JToken scnSceneResource)
        {
            var propName = "";

            if (scnSceneResource != null)
            {
                foreach (var prop in scnSceneResource.SelectToken("props"))
                {
                    if (prop.SelectToken("propId.id").Value<string>() == propID)
                    {
                        propName = "(" + propID + ") " + prop.SelectToken("propName").Value<string>();
                        break;
                    }
                }
            }

            return propName;
        }

        private static string GetActor(string actorID, JToken scnSceneResource)
        {
            var actorName = "";

            if (scnSceneResource != null)
            {
                foreach (var actor in scnSceneResource.SelectToken("actors"))
                {
                    if (actor.SelectToken("actorId.id").Value<string>() == actorID)
                    {
                        actorName = "(" + actorID + ") " + actor.SelectToken("actorName").Value<string>();
                        break;
                    }
                }
                foreach (var actor in scnSceneResource.SelectToken("playerActors"))
                {
                    if (actor.SelectToken("actorId.id").Value<string>() == actorID)
                    {
                        actorName = "(" + actorID + ") " + actor.SelectToken("playerName").Value<string>();
                        break;
                    }
                }
            }

            return actorName;
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

        private enum gameinteractionsChoiceType
        {
            QuestImportant = 1,
            AlreadyRead = 2,
            Inactive = 4,
            CheckSuccess = 8,
            CheckFailed = 16,
            InnerDialog = 32,
            PossessedDialog = 64,
            TimedDialog = 128,
            Blueline = 256,
            Pay = 512,
            Selected = 1024,
            Illegal = 2048,
            Glowline = 4096
        }

        private static bool isBitNActive(int integerValue, int bitNumber)
        {
            return (integerValue & (1 << bitNumber)) != 0;
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
