/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：MissionManager
// 创建者：Joe Mo
// 修改者列表：
// 创建日期：
// 模块描述：任务管理器(逻辑层)
//----------------------------------------------------------------*/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mogo.GameData;
using Mogo.Util;
using Mogo.Game;
using System.Text.RegularExpressions;
using System;
using Mogo.FSM;

namespace Mogo.Task
{
    public class TaskManager : IEventManager
    {
        public static readonly int SkyNPCID = 150000;
        public static readonly int InstanceNPCID = 149999; 

        public enum DialogueRelationship
        {
            You = 1,
            OlderToYounger = 2,
            YoungerToOlder = 3
        }

        protected EntityMyself theOwner;

        private int curNPCID = 0;

        private TaskData m_playerCurrentTask;
        public TaskData PlayerCurrentTask
        {
            get { return m_playerCurrentTask; }
        }

        protected bool isTalking = false;
        protected bool isFirstTask = false;

        // 回城延时
        public int delayHandleTaskID = -1;


        public TaskManager(EntityMyself owner, int taskMain)
        {
            theOwner = owner;
            InitAvatarTaskList(taskMain);

            AddListeners();
        }


        // 初始化任务
        private void InitAvatarTaskList(int taskMain)
        {
            if (taskMain < 1)
            {
                // to do key
                LoggerHelper.Debug("任务ID初始化不合法");
                taskMain = 1;
            }

            if (taskMain == 1)
                isFirstTask = true;

            if (TaskData.dataMap.ContainsKey(taskMain))
                m_playerCurrentTask = TaskData.dataMap[taskMain];

            if (MogoUIManager.Instance.m_NormalMainUI != null)
            {
                if (m_playerCurrentTask == null)
                {
                    NormalMainUIViewManager.Instance.SetAutoTaskText(LanguageData.GetContent(46854)); // 副本
                    NormalMainUIViewManager.Instance.SetAutoTaskIcon(IconData.dataMap.Get(31309).path);
                }
                else 
                {
                    NormalMainUIViewManager.Instance.SetAutoTaskText(LanguageData.GetContent(46855)); // 自动任务
                    NormalMainUIViewManager.Instance.SetAutoTaskIcon(IconData.dataMap.Get(31309).path);
                    ShowTaskTips();
                }
            }
        }


        // 增加监听
        public void AddListeners()
        {
            EventDispatcher.AddEventListener<int>(Events.TaskEvent.NPCInSight, OnNPCInSight);
            EventDispatcher.AddEventListener<int>(Events.TaskEvent.CloseToNPC, OnCloseToNPC);
            EventDispatcher.AddEventListener<int>(Events.TaskEvent.LeaveFromNPC, OnLeaveFromNPC);

            // EventDispatcher.AddEventListener<int, int>(Events.TaskEvent.LevelWin, OnLevelWin);
            // EventDispatcher.AddEventListener<int>(Events.TaskEvent.GuideDone, OnGuideDone);
            // EventDispatcher.AddEventListener<int>(Events.TaskEvent.GoToNextTask, OnGoingToNextTask);

            // to do
            EventDispatcher.AddEventListener("ShowMogoNormalMainUI", OnAutoShowMission);

            EventDispatcher.AddEventListener(Events.TaskEvent.TalkEnd, OnTalkEnd);
            EventDispatcher.AddEventListener(Events.TaskEvent.ShowRewardEnd, TaskInterimPeriod);

            EventDispatcher.AddEventListener(Events.TaskEvent.CheckNpcInRange, CheckNotSkyNPCInRange);
        }


        // 移除监听
        public void RemoveListeners()
        {
            EventDispatcher.RemoveEventListener<int>(Events.TaskEvent.NPCInSight, OnNPCInSight);
            EventDispatcher.RemoveEventListener<int>(Events.TaskEvent.CloseToNPC, OnCloseToNPC);
            EventDispatcher.RemoveEventListener<int>(Events.TaskEvent.LeaveFromNPC, OnLeaveFromNPC);
            
            // EventDispatcher.RemoveEventListener<int, int>(Events.TaskEvent.LevelWin, OnLevelWin);
            // EventDispatcher.RemoveEventListener<int>(Events.TaskEvent.GuideDone, OnGuideDone);
            // EventDispatcher.RemoveEventListener<int>(Events.TaskEvent.GoToNextTask, OnGoingToNextTask);

            // to do
            EventDispatcher.RemoveEventListener("ShowMogoNormalMainUI", OnAutoShowMission);

            EventDispatcher.RemoveEventListener(Events.TaskEvent.TalkEnd, OnTalkEnd);
            EventDispatcher.RemoveEventListener(Events.TaskEvent.ShowRewardEnd, TaskInterimPeriod);

            EventDispatcher.RemoveEventListener(Events.TaskEvent.CheckNpcInRange, CheckNotSkyNPCInRange);
        }


        // 自动弹处理
        private void OnAutoShowMission()
        {
            LoggerHelper.Debug("OnAutoShowMission");

            if (theOwner.IsNewPlayer)
                return;

            if (delayHandleTaskID != -1)
            {
                LoggerHelper.Debug("levelWin != -1");
                CheckTaskRewardShow();
                delayHandleTaskID = -1;
            }

            if (isFirstTask)
            {
                LoggerHelper.Debug("isFirstTask");
                isFirstTask = false;
                OnCloseToNPC(SkyNPCID);
            }
        }


        // 更新NPC进入视野时更改状态 to do
        public void OnNPCInSight(int npcID)
        {
            Mogo.Util.LoggerHelper.Debug("OnNPCInSight:" + npcID);
        }


        // 靠近NPC时候触发
        public void OnCloseToNPC(int npcID)
        {
            Mogo.Util.LoggerHelper.Debug("OnCloseToNpc:" + npcID);

            if (m_playerCurrentTask == null)
                return;

            if (m_playerCurrentTask.id == 0)
                return;

            if (isTalking || m_playerCurrentTask.npc != npcID)
                return;

            if (m_playerCurrentTask.conditionType != 0)
                return;

            curNPCID = npcID;
            isTalking = true;

            if (ControlStick.instance != null)
                ControlStick.instance.Reset();

            //Destroying GameObjects immediately is not permitted during physics trigger/contact or animation event callbacks. You must use Destroy instead.
            TimerHeap.AddTimer(0, 0, ShowTaskDialogue);
        }


        // 显示对话框
        private void ShowTaskDialogue()
        {
            Mogo.Util.LoggerHelper.Debug("ShowTaskDialogue");

            if (m_playerCurrentTask == null)
            {
                LoggerHelper.Debug("PlayerCurrentTask == null");
                return;
            }

            if (m_playerCurrentTask.text == null)
            {
                OnTalkEnd();
                //MogoUIManager.Instance.ShowMogoNormalMainUI();
                return;
            }

            if (m_playerCurrentTask.text.Count == 0)
            {
                OnTalkEnd();
                //MogoUIManager.Instance.ShowMogoNormalMainUI();
                return;
            }

            List<string> allName = new List<string>();
            List<string> allImg = new List<string>();
            List<string> allText = new List<string>();

            foreach (var item in m_playerCurrentTask.text)
            {
                int npcID = item.Value;
                string imageName = "", npcName = "";

                if (npcID == 0)
                {
                    int imageID = 0;
                    switch (MogoWorld.thePlayer.vocation)
                    {
                        case Vocation.Warrior:
                            imageID = 310;
                            break;
                        case Vocation.Assassin:
                            imageID = 311;
                            break;
                        case Vocation.Archer:
                            imageID = 312;
                            break;
                        case Vocation.Mage:
                            imageID = 313;
                            break;
                    }
                    imageName = IconData.dataMap.Get(imageID).path;
                    npcName = MogoWorld.thePlayer.name;
                }
                else
                {
                    imageName = IconData.dataMap.Get(NPCData.dataMap[npcID].dialogBoxImage).path;
                    // imageName = "body02";
                    npcName = LanguageData.GetContent(NPCData.dataMap[npcID].name);
                }

                string[] tempText = LanguageData.GetContent(item.Key).Split('_');
                for (int i = 0; i < tempText.Length; i++)
                {
                    tempText[i] = FormatTaskText(tempText[i]);
                }

                int count = tempText.Length;
                for (int i = 0; i < count; i++)
                {
                    allName.Add(npcName);
                    allImg.Add(imageName);
                    allText.Add(tempText[i]);
                }
            }

            ShowMogoTaskUI(allName.ToArray(), allImg.ToArray(), allText.ToArray(), MogoUIManager.Instance.m_NormalMainUI);
        }


        // 格式化输入的对话语言
        public string FormatTaskText(string taskText)
        {
            Mogo.Util.LoggerHelper.Debug("FormatTaskText");

            string text = taskText;
            Regex re = new Regex(@"\{\d+\}");

            foreach (Match matchData in re.Matches(text))
            {
                string value = matchData.Value;
                int flag = int.Parse(value.Replace("{", "").Replace("}", ""));

                switch (flag)
                {
                    case 0:
                        text = text.Replace(value, theOwner.name);
                        continue;
                    case 1:
                        text = text.Replace(value, theOwner.GetSexString(DialogueRelationship.You));
                        continue;
                    case 2:
                        text = text.Replace(value, theOwner.GetSexString(DialogueRelationship.OlderToYounger));
                        continue;
                    case 3:
                        text = text.Replace(value, theOwner.GetSexString(DialogueRelationship.YoungerToOlder));
                        continue;
                    default:
                        text = text.Replace(value, LanguageData.GetContent(flag));
                        continue;
                }
            }

            return text;
        }


        // 显示对话
        public void ShowMogoTaskUI(string[] npcName, string[] npcImage, string[] dialogs, GameObject baseUI = null)
        {
            Mogo.Util.LoggerHelper.Debug("ShowMogoTaskUI");

            MogoUIManager.Instance.ShowMogoTaskUI(TaskUILogicManager.Instance.SetTaskInfo, npcName, npcImage, dialogs, baseUI);
        }


        // 交谈结束
        public void OnTalkEnd()
        {
            Mogo.Util.LoggerHelper.Debug("OnTalkEnd");

            isTalking = false;
            if (m_playerCurrentTask == null)
                return;

            EventDispatcher.TriggerEvent(Events.NPCEvent.FrushIcon);
            EventDispatcher.TriggerEvent(Events.NPCEvent.TalkEnd, m_playerCurrentTask.npc);
            HandInTask(m_playerCurrentTask.id);
        }


        // 对话结束，上交任务
        public void HandInTask(int taskID)
        {
            Mogo.Util.LoggerHelper.Debug("OnGoingToNextTask " + taskID + "      " + curNPCID);

            // if (TaskData.dataMap[taskID].conditionType == 1)
                curNPCID = TaskData.dataMap[taskID].npc;

            #region 进度记录

                GameProcManager.HandInTask(taskID);

                #endregion

            theOwner.RpcCall("NPCReq", (uint)curNPCID, (uint)taskID, (uint)1);
        }


        // （回到王城）检测有无任务奖励框出现
        public void CheckTaskRewardShow()
        {
            Mogo.Util.LoggerHelper.Debug("CheckTaskRewardShow");

            if (ShowTaskReward())
            {
                //MogoGlobleUIManager.Instance.ShowTaskRewardTip(false);
                //MogoGlobleUIManager.Instance.OnTaskRewardTipOKUp();
                EventDispatcher.TriggerEvent(Events.TaskEvent.ShowRewardEnd);
            }
            else
                TaskInterimPeriod();
        }


        // 弹出奖励框
        public bool ShowTaskReward()
        {
            Mogo.Util.LoggerHelper.Debug("ShowTaskReward");

            Dictionary<int, int> items = GetCurrentTaskItemMessage();

            if (m_playerCurrentTask.money == 0
                && m_playerCurrentTask.exp == 0
                && items == null)
                return false;

            if (m_playerCurrentTask.money == 0
                && m_playerCurrentTask.exp == 0
                && items.Count == 0)
                return false;

            if (m_playerCurrentTask.money == 0
                && m_playerCurrentTask.exp == 0
                && items.Count == 1 && items.ContainsKey(0))
                return false;

            //List<string> itemPaths = new List<string>();
            //foreach (var itemID in itemIDs)
            //{
            //    var path = IconData.GetIconByItemID(itemID);
            //    if (path != String.Empty)
            //        itemPaths.Add(path);
            //}

            //if (m_playerCurrentTask.money == 0
            //    && m_playerCurrentTask.exp == 0
            //    && itemPaths.Count == 0)
            //    return false;

            List<int> itemIDs = new List<int>();
            List<int> itemNums = new List<int>();

            int countItem = 0;
            if (items != null)
            {
                foreach (var item in items)
                {
                    countItem++;
                    itemIDs.Add(item.Key);
                    itemNums.Add(item.Value);
                    if (countItem > 2)
                        break;
                }
            }

            MogoGlobleUIManager.Instance.ClearTaskRewardTipItemList();
            MogoGlobleUIManager.Instance.SetTaskRewardTipGold(m_playerCurrentTask.money.ToString());
            MogoGlobleUIManager.Instance.SetTaskRewardTipExp(m_playerCurrentTask.exp.ToString());
            MogoGlobleUIManager.Instance.SetTaskRewardTipItem(itemIDs);

            // 看看是不是这里需要注释
            // NormalMainUIViewManager.Instance.ShowAssistantDialog(false);

            Mogo.Util.LoggerHelper.Debug("ShowTaskReward End");

            return true;
        }


        // 获取不同职业奖励信息
        private Dictionary<int, int> GetCurrentTaskItemMessage()
        {
            switch (theOwner.vocation)
            {
                case Vocation.Warrior:
                    return m_playerCurrentTask.awards1;
                case Vocation.Assassin:
                    return m_playerCurrentTask.awards2;
                case Vocation.Archer:
                    return m_playerCurrentTask.awards3;
                case Vocation.Mage:
                    return m_playerCurrentTask.awards4;
                default:
                    return null;
            }
        }


        // 两个任务间的连接，这里处理上一个任务的奖励，接受下一个任务，弹出提示
        // 对于副本通关类型的来说，这里之前会被截停，直到回到王城才执行
        public void TaskInterimPeriod()
        {
            Mogo.Util.LoggerHelper.Debug("GoToNextTask " + m_playerCurrentTask.id);

            if (!TaskData.dataMap.ContainsKey(m_playerCurrentTask.nextId))
            {
                LoggerHelper.Debug("All Task Complete!");
                m_playerCurrentTask = null;

                if (MogoUIManager.Instance.m_NormalMainUI != null)
                {
                    NormalMainUIViewManager.Instance.SetAutoTaskText(LanguageData.GetContent(46854)); // 副本
                    NormalMainUIViewManager.Instance.SetAutoTaskIcon(IconData.dataMap.Get(31309).path);
                }

                return;
            }

            //MogoGlobleUIManager.Instance.ShowFinishedTaskSign(true);
            MogoFXManager.Instance.AttachTaskOverFX();

            // EventDispatcher.TriggerEvent(Events.OperationEvent.CheckFirstShow);
            GuideSystem.Instance.TriggerEvent<int>(GlobalEvents.FinishTask, m_playerCurrentTask.id);

            m_playerCurrentTask = TaskData.dataMap[m_playerCurrentTask.nextId];
            UpdateAutoTaskSign();

            GuideSystem.Instance.TriggerEvent<int>(GlobalEvents.GetTask, m_playerCurrentTask.id);

            CheckTaskNPCInRange();

            ShowTaskTips();

            int curTipNPC = 0;
            int nextTipNPC = 0;

            if (MogoWorld.thePlayer.CurrentTask != null)
            {
                if (MogoWorld.thePlayer.CurrentTask.isShowNPCTip == 1)
                {
                    nextTipNPC = MogoWorld.thePlayer.CurrentTask.npc;
                }

                if (MogoWorld.thePlayer.CurrentTask.conditionType == 1 && MogoWorld.thePlayer.CurrentTask.condition != null && MogoWorld.thePlayer.CurrentTask.condition.Count >= 3)
                {
                    curTipNPC = TaskData.dataMap.Get(MogoWorld.thePlayer.CurrentTask.condition[2]).npc;
                }
            }

            EventDispatcher.TriggerEvent(Events.TaskEvent.NPCSetSign, curTipNPC, nextTipNPC);
        }


        // 回城时由于物理碰撞的原因无法检测
        public void CheckTaskNPCInRange()
        {
            Mogo.Util.LoggerHelper.Debug("CheckTaskNPCInRange");

            if (m_playerCurrentTask.npc == SkyNPCID)
            {
                OnCloseToNPC(SkyNPCID);
            }
            else if (m_playerCurrentTask.npc != InstanceNPCID)
            {
                CheckNotSkyNPCInRange();
            }
        }


        public void CheckNotSkyNPCInRange()
        {
            if (theOwner.sceneId != MogoWorld.globalSetting.homeScene)
                return;

            if (m_playerCurrentTask == null)
                return;

            if (m_playerCurrentTask.npc == SkyNPCID || m_playerCurrentTask.npc == InstanceNPCID)
                return;

            Vector3 curNPCPosition = NPCManager.GetNPCPosition(m_playerCurrentTask.npc);
            EntityNPC curNPC = NPCManager.GetNPC(m_playerCurrentTask.npc);

            if (curNPCPosition != Vector3.zero)
            {
                // +1 for ensuring
                if (Vector3.Distance(curNPCPosition, theOwner.Transform.position) < NPCData.dataMap[m_playerCurrentTask.npc].colliderRange + 1
                    && MogoWorld.thePlayer.CurrentTask != null)
                {
                    OnCloseToNPC(m_playerCurrentTask.npc);
                    if (curNPC != null)
                        EventDispatcher.TriggerEvent(Events.NPCEvent.TurnToPlayer, MogoWorld.thePlayer.CurrentTask.npc, MogoWorld.thePlayer.Transform);
                }
            }
            else if (MogoWorld.thePlayer.CurrentMotionState != MotionState.WALKING && !isTalking)
            {
                LoggerHelper.Debug("CheckNotSkyNPCInRange");
                TimerHeap.AddTimer(20, 0, CheckNotSkyNPCInRange);
            }
        }


        // 处弹任务的助手提示
        public void ShowTaskTips()
        {
            Mogo.Util.LoggerHelper.Debug("ShowTaskTips");

            if (m_playerCurrentTask.tiptext == 0)
                return;

            NormalMainUIViewManager.Instance.SetAssistantDialogText(FormatTaskText(LanguageData.dataMap[m_playerCurrentTask.tiptext].content));
            NormalMainUIViewManager.Instance.ShowAssistantDialog(true);
            // NormalMainUILogicManager.Instance.SetAssistantDialogTextHide(8000);
        }


        public void UpdateAutoTaskSign()
        {
             // m_playerCurrentTask.autoIcon;
            NormalMainUIViewManager.Instance.SetAutoTaskIcon(IconData.dataMap.Get(m_playerCurrentTask.autoIcon).path);
        }


        public int GetTaskNearestPathPoint()
        {
            if (m_playerCurrentTask == null)
                return 0;

            return SubPathPoint.GetNearestSubPoint(theOwner.Transform.position, m_playerCurrentTask.pathPoint);
        }



        // 离开NPC
        public void OnLeaveFromNPC(int _npcId)
        {
            if (!isTalking)
                return;
            isTalking = false;
        }

        ///// <summary>
        ///// 当关卡胜利后查询进行任务中有无相关任务,改变任务状态
        ///// </summary>
        ///// <param name="_levelId"></param>
        //public void OnLevelWin(int _levelId, int _levelLevel)
        //{
        //    Mogo.Util.LoggerHelper.Debug("OnLevelWin");
        //    if (m_playerCurrentTask.conditionType == 1)
        //    {
        //        if (m_playerCurrentTask.condition[0] == _levelId && m_playerCurrentTask.condition[1] == _levelLevel)
        //        {
        //            // OnGoingToNextTask(playerCurrentTask.id);
        //            delayHandleTaskID = m_playerCurrentTask.id;
        //        }
        //    }
        //}


        //// 新手引导完成后
        //public void OnGuideDone(int _guidId)
        //{
        //    if (m_playerCurrentTask.conditionType == 2)
        //    {
        //        if (m_playerCurrentTask.condition[0] == _guidId)
        //        {
        //            OnGoingToNextTask(m_playerCurrentTask.id);
        //        }
        //    }
        //}
    }
}
