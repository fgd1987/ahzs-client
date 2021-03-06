/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：InstanceUILogicManager
// 创建者：MaiFeo
// 修改者列表：
// 创建日期：
// 模块描述：
//----------------------------------------------------------------*/

using UnityEngine;
using System.Collections;
using Mogo.Util;
using Mogo.GameData;
using System.Collections.Generic;
using System;
using System.Linq;
using Mogo.Mission;
using System.Text;
using Mogo.Game;

/// <summary>
/// 副本打开类型(用途)
/// </summary>
public enum MissionOpenType
{
    Quest = 0, // 当前任务
    Drop = 1, // 副本掉落
    LevelUpgradeGuide = 2, // 升级推荐
    FoggyAbyss = 3
}

static public class InstanceUIEvent
{
    // 点击[普通]格子
    public readonly static string OnChooseNormalGridUp = "InstanceUIEvent.OnChooseNormalGridUp";
    // 点击[迷雾深渊]格子
    public readonly static string OnChooseFoggyAbyssGridUp = "InstanceUIEvent.OnChooseFoggyAbyssGridUp";

    // 挑战[普通]副本
    public readonly static string OnEnterNormalUp = "InstanceUIEvent.OnEnterNormalUp";
    // 挑战[迷雾深渊]副本
    public readonly static string OnEnterFoggyAbyssUp = "InstanceUIEvent.OnEnterFoggyAbyssUp";

    // 主界面点击进入[迷雾深渊]副本
    public readonly static string OnNormalMainUIEnterFoggyAbyssUp = "InstanceUIEvent.OnNormalMainUIEnterFoggyAbyssUp";
}

public class InstanceUILogicManager
{
    #region 变量

    public static bool hasInit = false;

    private static InstanceUILogicManager m_instance;
    public static InstanceUILogicManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                m_instance = new InstanceUILogicManager();
            }

            return InstanceUILogicManager.m_instance;

        }
    }

    private int countInstanceGrid = 0;

    protected int mapID = 0;
    public int MapID 
    {
        get { return mapID; }

        protected set 
        { 
            mapID = value;
            if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
                InstanceMissionChooseUIViewManager.Instance.SetCurrentMapPage(value);
        }
    }
    private int gridID = -1;
    private int levelID = -1;

    private int taskOpenMissionGrid = -1;
    public int TaskOpenMissionGrid
    {
        get
        {
            return taskOpenMissionGrid;
        }
        set
        {
            taskOpenMissionGrid = value;
        }
    }

    private Dictionary<int, MercenaryInfo> mercenary;
    private int[] m_allMercenaryID;
    public int mercenaryID { get; protected set; }
    private int mercenaryCount = 0;
    public string mercenaryName { get; set; }

    private int lastLevelID;

    private List<List<int>> dropData;

    private int m_sweepTimes = 0;
    public int SweepTimes
    {
        get
        {
            return m_sweepTimes;
        }
        set
        {
            m_sweepTimes = value;
            InstanceLevelChooseUIViewManager.Instance.SetCleanTimes(m_sweepTimes);

            if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
            {
                InstanceMissionChooseUIViewManager.Instance.SetSweepNum(m_sweepTimes);
            }

                // Old Code
            else
            {
                NewInstanceUIChooseLevelViewManager.Instance.SetSweepNum(m_sweepTimes);
            }            
        }
    }

    #endregion

    public void Initialize()
    {
        MapID = 0;
        gridID = 0;
        levelID = 0;
        mercenaryID = 0;

        InstanceUIViewManager.Instance.INSTANCECHOOSEGRIDUP += OnChooseNormalGridUp;
        InstanceUIViewManager.Instance.CHOOSEGRIDCLOSEUP += OnChooseGridCloseUp;

        //InstanceUIViewManager.Instance.INSTANCECHOOSELEVEL0UP += OnChooseLevel0Up;
        //InstanceUIViewManager.Instance.INSTANCECHOOSELEVEL1UP += OnChooseLevel1Up;
        //InstanceUIViewManager.Instance.INSTANCECHOOSELEVEL2UP += OnChooseLevel2Up;
        //InstanceUIViewManager.Instance.CHOOSELEVELCLOSEUP += OnChooseLevelCloseUp;
        //InstanceUIViewManager.Instance.CHOOSELEVELUIBACKUP += OnChooseLevelUIBackUp;
        //InstanceUIViewManager.Instance.CHOOSELEVELUICLEANUP += OnChooseLevelCleanUp;
        //InstanceUIViewManager.Instance.INSTANCEENTERUP += OnEnterUp;

        InstanceLevelChooseUIViewManager.Instance.INSTANCECHOOSELEVEL0UP += OnChooseLevel0Up;
        InstanceLevelChooseUIViewManager.Instance.INSTANCECHOOSELEVEL1UP += OnChooseLevel1Up;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUICLOSEUP += OnChooseLevelUIBackUp;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUIBACKUP += OnChooseLevelUIBackUp;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUICLEANUP += OnChooseLevelCleanUp;        
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUIHELPUP += OnOpenChooseMercenaryUp;
        InstanceLevelChooseUIViewManager.Instance.SWITCHTOITEMREWARDUIUP += OnSwitchToItemRewardUIUp;
        InstanceLevelChooseUIViewManager.Instance.SWITCHTOCARDREWARDUIUP += OnSwitchToCardRewardUIUp;
        EventDispatcher.AddEventListener(InstanceUIEvent.OnEnterNormalUp, OnEnterNormalUp);
        EventDispatcher.AddEventListener(InstanceUIEvent.OnEnterFoggyAbyssUp, OnEnterFoggyAbyssUp);
        
        InstanceHelpChooseUIViewManager.Instance.INSTANCEHELPBACKUP += OnCloseChooseMercenaryUp;
        InstanceHelpChooseUIViewManager.Instance.INSTANCESELECTPLAYER += SelectMercenary;

        // InstanceUIViewManager.Instance.LEVEL2DIAMONDUP += GetResetTimes;
        InstanceUIViewManager.Instance.CANRESETMISSIONCONFIRM += OnCanResetMissionConfirmUp;
        InstanceUIViewManager.Instance.CANRESETMISSIONCANCEL += OnCanResetMissionCancelUp;
        InstanceUIViewManager.Instance.CANNOTRESETMISSIONCONFIRM += OnCanNotResetMissionConfirmUp;

        InstanceUIViewManager.Instance.CHOOSEGRIDUIMAPUP += OnChooseGridUIMapUp;
        InstanceUIViewManager.Instance.INSTANCEMAPUIMAPUP += OnMapUIMapUp;
        InstanceUIViewManager.Instance.INSTANCEMAPCLOSEUP += OnMapCloseUp;
        InstanceUIViewManager.Instance.INSTANCEMAPCHESTUP += OnMapChestUp;
        InstanceUIViewManager.Instance.INSTANCEMAPCHESTCONFIRMUP += OnMapChestConfirmUp;        

        //InstanceUIViewManager.Instance.PASSREWARDUIOKUP += OnPassRewardUIOKUp;
        //InstanceUIViewManager.Instance.INSTANCEFRIENDSHIPUP += OnFriendShipUp;

        //InstancePassUIViewManager.Instance.INSTANCEPASSOKUP += OnPassRewardUIOKUp;
        //InstancePassUIViewManager.Instance.INSTANCEPASSMAKEFRIENDUP += OnFriendShipUp;

        InstanceCleanUIViewManager.Instance.INSTANCECLEANCLEANUP += OnCleanUp;
        InstanceCleanUIViewManager.Instance.INSTANCECLEANCLEANREALUP += OnCleanRealUp;
        InstanceCleanUIViewManager.Instance.INSTANCECLEANCLOSEUP += OnCleanUICloseUp;
        InstanceCleanUIViewManager.Instance.INSTANCECLEANREWARDUP += OnRewadUp;

        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUIMISSIONFLAGUP += OnInstanceShowTask;
        InstanceTaskRewardUIViewManager.Instance.INSTANCETASKCLOSEUP += OnCloseInstanceTask;

        InstanceTreasureChestUIViewManager.Instance.TREASURECHESTGETUP += OnMapChestGetUp;
        InstanceTreasureChestUIViewManager.Instance.TREASURECHESTOKUP += OnMapChestOkUp;

        InstanceLevelChooseUIViewManager.Instance.RESETMISSIONUP += OnResetMissionUp;
        InstanceLevelChooseUIViewManager.Instance.CANRESETMISSIONWINDOWCONFIRMUP += OnCanResetMissionConfirmUp;

        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            InstanceMissionChooseUIViewManager.Instance.MAPPAGEMOVEDONE += OnMapPageMoveDone;            
            InstanceMissionChooseUIViewManager.Instance.BOSSCHESTBTNUP += OnBossChestButtonUp; // Boss宝箱
            InstanceMissionChooseUIViewManager.Instance.MISSIONRANDOMENTERUP += OnMissionRandomEnterUp;
        }

        EventDispatcher.AddEventListener("InstanceGridAwakeEnd", OnInstanceGridAwakeEnd);

        EventDispatcher.AddEventListener<int>("SetMapID", SetMapID);
        EventDispatcher.AddEventListener("BackToChooseUI", BackToChooseUI);

        EventDispatcher.AddEventListener<int>(InstanceUIEvent.OnChooseNormalGridUp, OnChooseNormalGridUp);
        EventDispatcher.AddEventListener(InstanceUIEvent.OnChooseFoggyAbyssGridUp, OnChooseFoggyAbyssGridUp);
        EventDispatcher.AddEventListener(InstanceUIEvent.OnNormalMainUIEnterFoggyAbyssUp, FoggyAbyssTipEnter);

        // to do
        // InstanceUIViewManager.Instance.ClearInstanceBodyGrid();

        foreach(var mData in MapUIMappingData.dataMap)
            InstanceUIViewManager.Instance.ModifyMapName(mData.Key);

        InstanceMissionChooseUIViewManager.Instance.SetMissionNormalName(LanguageData.GetContent(MapUIMappingData.dataMap[mapID].name));
    }


    public void Release()
    {
        MapID = 0;
        gridID = 0;
        levelID = 0;
        mercenaryID = 0;

        InstanceUIViewManager.Instance.INSTANCECHOOSEGRIDUP -= OnChooseNormalGridUp;
        InstanceUIViewManager.Instance.CHOOSEGRIDCLOSEUP -= OnChooseGridCloseUp;

        //InstanceUIViewManager.Instance.INSTANCECHOOSELEVEL0UP -= OnChooseLevel0Up;
        //InstanceUIViewManager.Instance.INSTANCECHOOSELEVEL1UP -= OnChooseLevel1Up;
        //InstanceUIViewManager.Instance.INSTANCECHOOSELEVEL2UP -= OnChooseLevel2Up;
        //InstanceUIViewManager.Instance.CHOOSELEVELCLOSEUP -= OnChooseLevelCloseUp;
        //InstanceUIViewManager.Instance.CHOOSELEVELUIBACKUP -= OnChooseLevelUIBackUp;
        //InstanceUIViewManager.Instance.CHOOSELEVELUICLEANUP -= OnChooseLevelCleanUp;
        //InstanceUIViewManager.Instance.INSTANCEENTERUP -= OnEnterUp;

        InstanceLevelChooseUIViewManager.Instance.INSTANCECHOOSELEVEL0UP -= OnChooseLevel0Up;
        InstanceLevelChooseUIViewManager.Instance.INSTANCECHOOSELEVEL1UP -= OnChooseLevel1Up;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUICLOSEUP -= OnChooseLevelUIBackUp;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUIBACKUP -= OnChooseLevelUIBackUp;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUICLEANUP -= OnChooseLevelCleanUp;
        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUIHELPUP -= OnOpenChooseMercenaryUp;
        InstanceLevelChooseUIViewManager.Instance.SWITCHTOITEMREWARDUIUP -= OnSwitchToItemRewardUIUp;
        InstanceLevelChooseUIViewManager.Instance.SWITCHTOCARDREWARDUIUP -= OnSwitchToCardRewardUIUp;
        EventDispatcher.RemoveEventListener(InstanceUIEvent.OnEnterNormalUp, OnEnterNormalUp);
        EventDispatcher.RemoveEventListener(InstanceUIEvent.OnEnterFoggyAbyssUp, OnEnterFoggyAbyssUp);

        
        InstanceHelpChooseUIViewManager.Instance.INSTANCEHELPBACKUP -= OnCloseChooseMercenaryUp;
        InstanceHelpChooseUIViewManager.Instance.INSTANCESELECTPLAYER -= SelectMercenary;

        // InstanceUIViewManager.Instance.LEVEL2DIAMONDUP -= GetResetTimes;
        InstanceUIViewManager.Instance.CANRESETMISSIONCONFIRM -= OnCanResetMissionConfirmUp;
        InstanceUIViewManager.Instance.CANRESETMISSIONCANCEL -= OnCanResetMissionCancelUp;
        InstanceUIViewManager.Instance.CANNOTRESETMISSIONCONFIRM -= OnCanNotResetMissionConfirmUp;

        InstanceUIViewManager.Instance.CHOOSEGRIDUIMAPUP -= OnChooseGridUIMapUp;
        InstanceUIViewManager.Instance.INSTANCEMAPUIMAPUP -= OnMapUIMapUp;
        InstanceUIViewManager.Instance.INSTANCEMAPCLOSEUP -= OnMapCloseUp;
        InstanceUIViewManager.Instance.INSTANCEMAPCHESTUP -= OnMapChestUp;
        InstanceUIViewManager.Instance.INSTANCEMAPCHESTCONFIRMUP -= OnMapChestConfirmUp;

        //InstanceUIViewManager.Instance.PASSREWARDUIOKUP -= OnPassRewardUIOKUp;
        //InstanceUIViewManager.Instance.INSTANCEFRIENDSHIPUP -= OnFriendShipUp;

        InstancePassUIViewManager.Instance.INSTANCEPASSOKUP -= OnPassRewardUIOKUp;
        InstancePassUIViewManager.Instance.INSTANCEPASSMAKEFRIENDUP -= OnFriendShipUp;

        InstanceCleanUIViewManager.Instance.INSTANCECLEANCLEANUP -= OnCleanUp;
        InstanceCleanUIViewManager.Instance.INSTANCECLEANCLEANREALUP -= OnCleanRealUp;
        InstanceCleanUIViewManager.Instance.INSTANCECLEANCLOSEUP -= OnCleanUICloseUp;
        InstanceCleanUIViewManager.Instance.INSTANCECLEANREWARDUP -= OnRewadUp;

        InstanceLevelChooseUIViewManager.Instance.CHOOSELEVELUIMISSIONFLAGUP -= OnInstanceShowTask;
        InstanceTaskRewardUIViewManager.Instance.INSTANCETASKCLOSEUP -= OnCloseInstanceTask;

        InstanceTreasureChestUIViewManager.Instance.TREASURECHESTGETUP -= OnMapChestGetUp;
        InstanceTreasureChestUIViewManager.Instance.TREASURECHESTOKUP -= OnMapChestOkUp;

        InstanceLevelChooseUIViewManager.Instance.RESETMISSIONUP -= OnResetMissionUp;
        InstanceLevelChooseUIViewManager.Instance.CANRESETMISSIONWINDOWCONFIRMUP -= OnCanResetMissionConfirmUp;

        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            InstanceMissionChooseUIViewManager.Instance.MAPPAGEMOVEDONE -= OnMapPageMoveDone;
            InstanceMissionChooseUIViewManager.Instance.BOSSCHESTBTNUP -= OnBossChestButtonUp; // Boss宝箱
            InstanceMissionChooseUIViewManager.Instance.MISSIONRANDOMENTERUP -= OnMissionRandomEnterUp;
        }

        EventDispatcher.RemoveEventListener("InstanceGridAwakeEnd", OnInstanceGridAwakeEnd);

        EventDispatcher.RemoveEventListener<int>("SetMapID", SetMapID);
        EventDispatcher.RemoveEventListener("BackToChooseUI", BackToChooseUI);

        EventDispatcher.RemoveEventListener<int>(InstanceUIEvent.OnChooseNormalGridUp, OnChooseNormalGridUp);
        EventDispatcher.RemoveEventListener(InstanceUIEvent.OnChooseFoggyAbyssGridUp, OnChooseFoggyAbyssGridUp);
        EventDispatcher.RemoveEventListener(InstanceUIEvent.OnNormalMainUIEnterFoggyAbyssUp, FoggyAbyssTipEnter);
    }


    #region 关卡相关

    // 格子加载完毕
    void OnInstanceGridAwakeEnd()
    {
        countInstanceGrid++;
        if (countInstanceGrid == 9)
        {
            hasInit = true;

            countInstanceGrid = 0;
            
            UpdateGridMessage();
            UpdateTopMessage();
        }
    }


    // 刷新格子
    public void UpdateGridMessage()
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            InstanceMissionChooseUIViewManager.Instance.HideChooseLevelGridList(MapID);

            var gridShape = MapUIMappingData.dataMap[MapID].gridShape;

            int index = 0;
            foreach (var grid in gridShape)
            {
                if (grid.Value == 0)
                {
                    InstanceMissionChooseUIViewManager.Instance.SetGridEnable(MapID, index, false);
                }
                else if (grid.Value == 1)
                {
                    InstanceMissionChooseUIViewManager.Instance.SetGridEnable(MapID, index, true);
                }

                index++;
            }

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMissionEnable);
            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMissionName);
            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMissionStar);
        }

            // Old Code
        else
        {
            NewInstanceUIChooseLevelViewManager.Instance.ClearChooseLevelGridList();

            // InstanceUIViewManager.Instance.ModifyMapName(MapID);

            var gridShape = MapUIMappingData.dataMap[MapID].gridShape;

            int index = 0;
            foreach (var grid in gridShape)
            {
                if (grid.Value == 0)
                {
                    NewInstanceUIChooseLevelViewManager.Instance.SetGridEnable(index, false);
                }
                else if (grid.Value == 1)
                {
                    NewInstanceUIChooseLevelViewManager.Instance.SetGridEnable(index, true);
                }

                index++;
            }

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMissionEnable);
            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMissionName);
            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMissionStar);
            EventDispatcher.TriggerEvent(Events.InstanceEvent.GetSweepTimes);
            GetResetTimes();
        }       
    }


    public void UpdateTopMessage()
    {
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetBossChestRewardGotMessage);
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetChestRewardGotMessage);
        EventDispatcher.TriggerEvent(Events.InstanceEvent.GetSweepTimes);
        GetResetTimes();
    }


    // 设置格子名字
    public void SetGridName(int gridID, string name)
    {
        // InstanceUIViewManager.Instance.SetInstanceChooseGridName(gridID, name);
    }

    // 设置格子名字
    public void SetGridName(int mapID, int gridID, string name)
    {
        // InstanceUIViewManager.Instance.SetInstanceChooseGridName(mapID, gridID, name);
    }


    // 设置格子图片
    public void SetGridImage(int gridID, string image)
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {

        }

            // Old Code
        else
        {
            NewInstanceUIChooseLevelViewManager.Instance.SetGridIcon(gridID, image);
        }        
    }

    // 设置格子图片
    public void SetGridImage(int mapID, int gridID, string image)
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            InstanceMissionChooseUIViewManager.Instance.SetGridIcon(mapID, gridID, image);
        }        
    }

    #region 点击关卡格子

    // 点击选关格子的共同显示：设置选难度UI关卡名字，请求关卡、通关次数、星星数，清除上次设置的关卡格子和相关数据，请求佣兵数据，设置默认值，显示
    void OnChooseNormalGridUp(int id)
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            gridID = id;

            foreach (var mData in MapUIMappingData.dataMap)
                InstanceMissionChooseUIViewManager.Instance.HideGridTip(mData.Key);
            //TaskOpenMissionGrid = -1;


            MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);

            // 切换模式
            InstanceLevelChooseUIViewManager.Instance.ChangeMode(MissionType.Normal);

            // 新界面的推荐等级,真的要显示的话要放在下一层
            InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRecommendLevel(String.Empty, false);

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelEnable, GetMissionId(gridID));

            // 这里要写在后面设置值之前 
            SetDefalutLevel(GetMissionId(gridID));

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetDrops, GetMissionId(gridID));

            // 可能可以增加默认选择的余地
            ShowLevelMessage(levelID);
        }

            // Old Code
        else
        {
            gridID = id;

            NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();
            //TaskOpenMissionGrid = -1;

            MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);

            // 新界面的推荐等级,真的要显示的话要放在下一层
            InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRecommendLevel(String.Empty, false);

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelEnable, GetMissionId(gridID));

            // 这里要写在后面设置值之前 
            SetDefalutLevel(GetMissionId(gridID));

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetDrops, GetMissionId(gridID));

            // 可能可以增加默认选择的余地
            ShowLevelMessage(levelID);
        }        
    }


    // 点击选关格子（任务打开副本）的共同显示：设置选难度UI关卡名字，请求关卡、通关次数、星星数，清除上次设置的关卡格子和相关数据，请求佣兵数据，设置默认值，显示
    void OnChooseNormalGridUp(int id, int defaultLevelSet)
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            gridID = id;

            foreach (var mData in MapUIMappingData.dataMap)
                InstanceMissionChooseUIViewManager.Instance.HideGridTip(mData.Key);
            TaskOpenMissionGrid = -1;

            MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);

            // 切换模式
            InstanceLevelChooseUIViewManager.Instance.ChangeMode(MissionType.Normal);

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelEnable, GetMissionId(gridID));

            // 新界面的推荐等级,真的要显示的话要放在下一层
            InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRecommendLevel(String.Empty, false);

            // 这里要写在后面设置值之前 
            SetDefalutLevel(GetMissionId(gridID));

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetDrops, GetMissionId(gridID));

            // 可能可以增加默认选择的余地
            ShowLevelMessage(levelID);
        }

            // Old Code
        else
        {
            gridID = id;

            NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();
            TaskOpenMissionGrid = -1;

            MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelEnable, GetMissionId(gridID));

            // 新界面的推荐等级,真的要显示的话要放在下一层
            InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRecommendLevel(String.Empty, false);

            // 这里要写在后面设置值之前 
            SetDefalutLevel(GetMissionId(gridID));

            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetDrops, GetMissionId(gridID));

            // 可能可以增加默认选择的余地
            ShowLevelMessage(levelID);
        }       
    }

    #endregion

    // 针对不同难度的个性化显示请求和设置
    public void ShowLevelMessage(int levelSet)
    {
        int missionIDTemp = GetMissionId(gridID);

        // 是否显示难度选择按钮
        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChoose(true);
        // 设置难度选择按钮选中
        InstanceLevelChooseUIViewManager.Instance.SelectedBtnLevelChoose(levelSet - 1);

        if (MogoWorld.thePlayer != null
            && MogoWorld.thePlayer.CurrentTask != null
            && MogoWorld.thePlayer.CurrentTask.conditionType == 1
            && MogoWorld.thePlayer.CurrentTask.condition[0] == missionIDTemp
            && MogoWorld.thePlayer.CurrentTask.condition[1] == levelSet)
        {
            InstanceLevelChooseUIViewManager.Instance.ShowMissionFlagBtn(true);
        }
        else
        {
            InstanceLevelChooseUIViewManager.Instance.ShowMissionFlagBtn(false);
        }

        switch (CurrentMissionOpenType)
        {
            case MissionOpenType.Drop:
                {
                    if (MogoWorld.thePlayer != null && missionIDTemp == MissionOpenMission)
                    {
                        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChooseTip(true, MissionOpenLevel - 1, GetTipTextID());
                    }
                    else
                    {
                        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChooseTip(false);
                    }
                }
                break;

            case MissionOpenType.LevelUpgradeGuide:
                {
                    if (MogoWorld.thePlayer != null && missionIDTemp == MissionOpenMission)
                    {
                        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChooseTip(true, MissionOpenLevel - 1, GetTipTextID());
                    }
                    else
                    {
                        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChooseTip(false);
                    }
                }
                break;

            case MissionOpenType.Quest:
            default:
                {
                    if (MogoWorld.thePlayer != null
                     && MogoWorld.thePlayer.CurrentTask != null
                     && MogoWorld.thePlayer.CurrentTask.conditionType == 1
                     && MogoWorld.thePlayer.CurrentTask.condition[0] == missionIDTemp)
                    {
                        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChooseTip(true, MogoWorld.thePlayer.CurrentTask.condition[1] - 1, GetTipTextID());
                    }
                    else
                    {
                        InstanceLevelChooseUIViewManager.Instance.ShowBtnLevelChooseTip(false);
                    }
                }
                break;
        }       

        InstanceLevelChooseUIViewManager.Instance.SetInstanceChooseGridTitle(LanguageData.dataMap.Get(MapUIMappingData.dataMap.Get(MapID).gridName.Get(gridID)).content, levelSet -1);

        // 新界面体力消耗
        KeyValuePair<int, MissionData> tempMissionData = MissionData.dataMap.FirstOrDefault(t => t.Value.mission == missionIDTemp && t.Value.difficulty == levelSet);
        if (tempMissionData.Value != null && MogoWorld.thePlayer != null)
        {
            if (tempMissionData.Value.level > MogoWorld.thePlayer.level)
            {
                InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRecommendLevel(LanguageData.GetContent(46992) + tempMissionData.Value.level);
                InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelCostVP(tempMissionData.Value.energy, false);
            }
            else
            {
                InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRecommendLevel(LanguageData.GetContent(46992) + tempMissionData.Value.level, false);
                InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelCostVP(tempMissionData.Value.energy);
            }
        }

        //ShowDropsGridData();

        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelTime, missionIDTemp, levelSet);
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelStar, missionIDTemp, levelSet);
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateLevelRecord, missionIDTemp, levelSet);
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMercenaryButton, missionIDTemp, levelSet);

        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.ShowCard, GetMissionId(gridID), levelID);
    }


    // 通过格子ID（结合当前地图）获取关卡ID
    private int GetMissionId(int gridID)
    {
        return MapUIMappingData.dataMap.Get(MapID).grid.Get(gridID);
    }


    // 格子界面的关闭
    public void OnChooseGridCloseUp()
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            foreach (var mData in MapUIMappingData.dataMap)
                InstanceMissionChooseUIViewManager.Instance.HideGridTip(mData.Key);
            TaskOpenMissionGrid = -1;
            MogoUIManager.Instance.ShowMogoNormalMainUI();
        }

            // Old Code
        else
        {
            NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();
            TaskOpenMissionGrid = -1;
            MogoUIManager.Instance.ShowMogoNormalMainUI();
        }      
    }

    #endregion


    #region 大地图

    // 点击选地图：设置地图名字
    void OnChooseGridUIMapUp()
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            MogoUIManager.Instance.ShowInstanceMissionChooseUI(false);
        }
        else
        {
            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateMapName);
            MogoUIManager.Instance.ShowNewInstanceChooseMissionUI(false);
        }        

        MogoUIManager.Instance.ShowMogoInstanceUI(true);
        InstanceUIViewManager.Instance.ShowInstanceMapUI(true);
    }


    // 设置地图名字
    public void SetMapName(int mapID, string name)
    {
        InstanceUIViewManager.Instance.SetMapName(mapID, name);
    }


    // 设置地图：隐藏上一个地图的标记，设置当前地图的标志标志，更新选关页标题为当前地图名
    public void SetMapID(int id)
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            if (id == MapID)
                return;

            if (id != MapID)
            {
                // InstanceUIViewManager.Instance.HideCurrentTaskMark();
                foreach (var item in MapUIMappingData.dataMap)
                    InstanceMissionChooseUIViewManager.Instance.HideGridTip(item.Key);
                TaskOpenMissionGrid = -1;
            }

            MapID = id;
            SystemConfig.Instance.LastMap = id;
            SystemConfig.SaveConfig();

            InstanceUIViewManager.Instance.SetMark(MapID);
            // InstanceUIViewManager.Instance.ModifyMapName(mapID);
            //foreach (var item in MapUIMappingData.dataMap)
            //    InstanceMissionChooseUIViewManager.Instance.ResetLevelGridPos(item.Key, item.Key + 1);

            InstanceMissionChooseUIViewManager.Instance.SetMissionNormalName(LanguageData.GetContent(MapUIMappingData.dataMap[mapID].name));

            UpdateGridMessage();
        }

            // Old Code
        else
        {
            if (id != MapID)
            {
                // InstanceUIViewManager.Instance.HideCurrentTaskMark();
                NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();
                TaskOpenMissionGrid = -1;
            }

            MapID = id;
            SystemConfig.Instance.LastMap = id;
            SystemConfig.SaveConfig();

            InstanceUIViewManager.Instance.SetMark(MapID);
            // InstanceUIViewManager.Instance.ModifyMapName(MapID);
            NewInstanceUIChooseLevelViewManager.Instance.ResetLevelGridPos(MapID + 1);

            UpdateGridMessage();
        }      
    }


    // 选择地图
    void OnMapUIMapUp(int id)
    {
        LoggerHelper.Debug(id);
    }


    // 关闭地图UI
    void OnMapCloseUp()
    {
        BackToChooseUI();
    }


    // 从选地图处返回：设置关卡格子名字
    public void BackToChooseUI()
    {
        InstanceUIViewManager.Instance.ShowInstanceMapUI(false);
        MogoUIManager.Instance.ShowMogoInstanceUI(false);

        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            MogoUIManager.Instance.ShowInstanceMissionChooseUI(true);
        }
        else
        {
            MogoUIManager.Instance.ShowNewInstanceChooseMissionUI(true);
        }        
    }

    #endregion


    #region 难度相关

    public int SetDefalutLevel(int missionID)
    {
        if (MogoWorld.thePlayer == null)
            return levelID;

        if (CurrentMissionOpenType == MissionOpenType.Drop)
        {
            if(MogoWorld.thePlayer != null
                && MissionOpenMission == missionID)
            {
                levelID = MissionOpenLevel;
                return levelID;
            }       
        }
        else if (CurrentMissionOpenType == MissionOpenType.Quest)
        {
            if (MogoWorld.thePlayer != null
                && MogoWorld.thePlayer.CurrentTask != null
                && MogoWorld.thePlayer.CurrentTask.conditionType == 1
                && MogoWorld.thePlayer.CurrentTask.condition[0] == missionID)
            {
                levelID = MogoWorld.thePlayer.CurrentTask.condition[1];
                return levelID;
            }
        }

        if (MogoWorld.thePlayer.CheckCurrentMissionEasyComplete(missionID))
            levelID = 2;
        else
            levelID = 1; 

        return levelID;
    }

    //点击低难度
    void OnChooseLevel0Up()
    {
        levelID = 1;
        ShowLevelMessage(1);
        ShowDropsGridData();
    }


    //点击中难度
    void OnChooseLevel1Up()
    {
        levelID = 2;
        ShowLevelMessage(2);
        ShowDropsGridData();
    }


    //点击高难度
    void OnChooseLevel2Up()
    {
        levelID = 3;
    }


    // 设置数据
    public void SetDropsGridData(List<List<int>> theDataID)
    {
        dropData = theDataID;
        if (MogoUIManager.Instance.CurrentUI == MogoUIManager.Instance.m_InstanceLevelChooseUI)
            ShowDropsGridData();
    }


    // 显示设置的数据
    public void ShowDropsGridData()
    {
        List<string> showImageName = new List<string>();

        if (dropData != null && dropData.Count >= 2)
        {
            List<int> showID = new List<int>();

            if (levelID - 1 < 0 && lastLevelID - 1 >= 0)
                showID = dropData[lastLevelID - 1];
            else
                showID = dropData[levelID - 1];

            if (showID != null && showID.Count > 0)
            {
                foreach (var id in showID)
                {
                    showImageName.Add(ItemParentData.GetItem(id).Icon);
                }

                //InstanceLevelChooseUIViewManager.Instance.SetRewardItemID(showID);
                InstanceLevelChooseUIViewManager.Instance.SetRewardItemIDList(showID);
                // InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRewardItemImage(showImageName);
                InstanceLevelChooseUIViewManager.Instance.SetDropInfo(LanguageData.GetContent(300100) + InventoryManager.Instance.FormatDropName(ItemParentData.GetItem(showID[0])));
            }
            else
            {
                //InstanceLevelChooseUIViewManager.Instance.SetRewardItemID(new List<int>());
                InstanceLevelChooseUIViewManager.Instance.SetRewardItemIDList(new List<int>());
                //InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRewardItemImage(showImageName);
                InstanceLevelChooseUIViewManager.Instance.SetDropInfo(String.Empty);
            }
        }
        else
        {
            //InstanceLevelChooseUIViewManager.Instance.SetRewardItemID(new List<int>());
            InstanceLevelChooseUIViewManager.Instance.SetRewardItemIDList(new List<int>());
            //InstanceLevelChooseUIViewManager.Instance.SetInstanceLevelRewardItemImage(showImageName);
            InstanceLevelChooseUIViewManager.Instance.SetDropInfo(String.Empty);
        }
    }


    // 点击关闭选择难度界面：清除当前雇佣兵列表，隐藏当前任务标记（可以不隐藏），关闭当前UI，打开选择关卡UI
    public void OnChooseLevelCloseUp()
    {
        //InstanceUIViewManager.Instance.HideCurrentTaskMark();
        // NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();

        //InstanceUIViewManager.Instance.ShowInstanceLevelChooseUI(false);
        //InstanceUIViewManager.Instance.ShowInstanceChooseUI(true);

        MogoUIManager.Instance.ShowMogoNormalMainUI();
    }


    // 点击返回，从选择难度界面返回关卡界面：刷新名字，清除当前雇佣兵列表，隐藏当前任务标记（可以不隐藏），关闭当前UI，打开选择关卡UI
    public void OnChooseLevelUIBackUp()
    {
        // to do
        UpdateGridMessage();
        UpdateTopMessage();

        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            if (TaskOpenMissionGrid != -1)
            {
                InstanceMissionChooseUIViewManager.Instance.ShowGridTip(mapID, TaskOpenMissionGrid, true, GetTipTextID());
            }

            MogoUIManager.Instance.ShowInstanceMissionChooseUI(true);
        }

            // Old Code
        else
        {
            if (TaskOpenMissionGrid != -1)
                NewInstanceUIChooseLevelViewManager.Instance.ShowGridTip(TaskOpenMissionGrid, true);

            MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(false);
            MogoUIManager.Instance.ShowNewInstanceChooseMissionUI(true);
        }        
    }


    // 选择扫荡
    void OnChooseLevelCleanUp()
    {
        Debug.Log("OnChooseLevelCleanUp");

        if (MogoWorld.thePlayer.VipLevel == 0)
        {
            MogoMsgBox.Instance.ShowFloatingText(LanguageData.dataMap[820].content);
            return;
        }

        if (SweepTimes == 0)
        {
            MogoMsgBox.Instance.ShowFloatingText(LanguageData.dataMap[821].content);
            return;
        }

        // 关卡体力消耗
        int costVP = (MissionData.dataMap.First(t => t.Value.mission == GetMissionId(gridID) && t.Value.difficulty == levelID).Value.energy);
        if (costVP > MogoWorld.thePlayer.energy)
        {
            MogoMsgBox.Instance.ShowFloatingText(LanguageData.dataMap[822].content);
            MogoUIManager.Instance.ShowEnergyNoEnoughUI(null);
            return;
        }

        // 进入扫荡界面
        InstanceCleanUIViewManager.Instance.SetTitle(LanguageData.dataMap.Get(MapUIMappingData.dataMap.Get(MapID).gridName.Get(gridID)).content, levelID - 1);

        MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(false);
        MogoUIManager.Instance.LoadMogoInstanceCleanUI(true);

        // 一进入扫荡界面就开始扫荡
        EventDispatcher.TriggerEvent<int, int>(Events.InstanceEvent.GetSweepMissionList, GetMissionId(gridID), levelID);
    }


    // 点击进入[普通副本]：触发进入，设置下次默认难度（未使用），隐藏当前任务标记，隐藏当前UI
    void OnEnterNormalUp()
    {
        if (levelID == 1 || levelID == 2 || levelID == 3)
        {
            EventDispatcher.TriggerEvent(Events.InstanceEvent.InstanceSelected, GetMissionId(gridID), levelID);

            lastLevelID = levelID;
            levelID = -1;

            //InstanceUIViewManager.Instance.gameObject.SetActive(false);

            // InstanceUIViewManager.Instance.HideCurrentTaskMark();
            //NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();

            //MogoUIManager.Instance.ShowNewInstanceChooseMissionUI(false);

            //InstanceUIViewManager.Instance.ShowInstanceLevelChooseUI(false);
            // InstanceUIViewManager.Instance.ShowInstanceChooseUI(true);
        }
        else
        {
            // MogoMsgBox.Instance.ShowFloatingText("Please Choose Level");
            return;
        }
    }

    public void EnterFailed()
    {
        levelID = lastLevelID;
    }

    public void EnterSuccess()
    {
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            foreach (var mData in MapUIMappingData.dataMap)
                InstanceMissionChooseUIViewManager.Instance.HideGridTip(mData.Key);
            taskOpenMissionGrid = -1;
            MogoUIManager.Instance.ShowInstanceMissionChooseUI(false);
        }

            // Old Code
        else
        {
            NewInstanceUIChooseLevelViewManager.Instance.HideGridTip();
            taskOpenMissionGrid = -1;
            MogoUIManager.Instance.ShowNewInstanceChooseMissionUI(false);
        }        
    }

    /// <summary>
    /// 点击回退按钮
    /// </summary>
    private void OnSwitchToItemRewardUIUp()
    {

    }

    /// <summary>
    /// 点击查看翻牌奖励按钮
    /// </summary>
    private void OnSwitchToCardRewardUIUp()
    {

    }

    #endregion

     
    #region 雇佣兵

    // 打开雇佣兵界面
    public void OnOpenChooseMercenaryUp()
    {
        MogoUIManager.Instance.LoadMogoInstanceHelpChooseUI(true);

        ClearMercenaryList();
        mercenaryCount = 0;

        // EventDispatcher.TriggerEvent<int>(Events.InstanceEvent.GetMercenaryInfo, GetMissionId(gridID));
        InstanceHelpChooseUIViewManager.Instance.UpdateMercenaryList(mercenary);
    }


    // 关闭雇佣兵界面
    protected void OnCloseChooseMercenaryUp()
    {
        MogoUIManager.Instance.LoadMogoInstanceHelpChooseUI(false);
        MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);
    }


    // 设置雇佣兵格子
    public void SetMercenaryGrid(Dictionary<int, Mogo.Mission.MercenaryInfo> theMercenaryInfo)
    {
        if (theMercenaryInfo != null && theMercenaryInfo.Count != 0)
        {
            mercenary = theMercenaryInfo;

            // 默认选一个，下面函数存在异步问题
            // InstanceUIViewManager.Instance.UpdateMercenaryList(theMercenaryInfo);
            // InstanceHelpChooseUIViewManager.Instance.UpdateMercenaryList(theMercenaryInfo);

            mercenaryID = 1;
            mercenaryName = mercenary[mercenaryID].name;
        }
        else
        {
            // to do 
            mercenaryID = 0;
            mercenaryName = String.Empty;
        }
    }


    // 计算格子创建以便设置雇佣兵默认选中
    public void CountMercenaryGridCreate()
    {
        mercenaryCount++;
        if (mercenary != null && mercenaryCount == mercenary.Count)
        {
            // 设置默认选中
            // InstanceUIViewManager.Instance.SetChooseHelperByIndex(mercenaryID - 1);
            InstanceHelpChooseUIViewManager.Instance.SetChooseHelperByIndex(mercenaryID - 1);
        }
    }


    // 选择雇佣兵
    public void SelectMercenary(int id)
    {
        mercenaryID = id;
        mercenaryName = mercenary[mercenaryID].name;
    }


    // 重置雇佣兵ID
    public void ResetMercenaryID()
    {
        mercenaryID = 0;
        mercenaryName = "";
    }


    // 清除佣兵格子资料
    public void ClearMercenaryList()
    {
        // InstanceUIViewManager.Instance.ClearMercenaryList();
        InstanceHelpChooseUIViewManager.Instance.ClearMercenaryList();
    }

    #endregion


    #region 通关界面

    // 增加奖励格子，回调里面设置奖励UI的奖励
    public void SetInstanceRewardUIReward(List<int> ids, List<int> counts, List<string> itemNames)
    {      
        InstancePassRewardUIViewManager.Instance.AddRewardItem(ids.Count, () =>
        {
            SetInstanceRewardUIGridRewardMessage(ids, counts, itemNames);
        });
    }


    // 设置奖励格子的奖励信息
    public void SetInstanceRewardUIGridRewardMessage(List<int> ids, List<int> counts, List<string> itemNames)
    {
        for (int i = 0; i < ids.Count; i++)
        {
            InstancePassRewardUIViewManager.Instance.SetRewardItemData(i, ids[i], counts[i], itemNames[i]);
        }
    }

    #endregion


    #region 评分界面

    // 界面显示
    public void SetPassMessage(int minutes, int second, int maxCombo, float scorePoint, int starNum)
    {
        InstancePassUIViewManager.Instance.SetPassTime(minutes, second);
        InstancePassUIViewManager.Instance.SetMaxBatter(maxCombo);
        InstancePassUIViewManager.Instance.SetScore((int)scorePoint);
        InstancePassUIViewManager.Instance.ShowPassStar(starNum);

        //if (mercenary != null && mercenary.ContainsKey(mercenaryID))
        //{
        //    if (mercenary[mercenaryID].isFriend == 0)
        //        InstancePassUIViewManager.Instance.SetFriendState(3, mercenaryName);
        //    else
        //        InstancePassUIViewManager.Instance.SetFriendState(2, mercenaryName);
        //}
        //else
        //{
              InstancePassUIViewManager.Instance.SetFriendState(1);
        //}
    }


    // 点击好友按钮：好友申请 / 提升好感度，隐藏按钮/显示“赞”并隐藏按钮
    void OnFriendShipUp()
    {
        if (mercenary != null && mercenary.ContainsKey(mercenaryID))
        {
            if (mercenary[mercenaryID].isFriend == 0)
            {
                FriendManager.Instance.AddFriendByID(mercenary[mercenaryID].dbid);
                // InstanceUIViewManager.Instance.HideFriendShipGoodButton();
            }
            else
            {
                EventDispatcher.TriggerEvent(Events.InstanceEvent.AddFriendDegree);
                // InstanceUIViewManager.Instance.ShowGoodAndHideFriendShipGoodButton();
            }
        }
    }


    // 更新胜利之后界面显示
    //public void UpdateFriendShip(string theName)
    //{
    //    if (mercenary != null && mercenary.ContainsKey(mercenaryID))
    //    {
    //        if (mercenary[mercenaryID].isFriend == 0)
    //            InstanceUIViewManager.Instance.RequireFriendShip(mercenaryName);
    //        else
    //            InstanceUIViewManager.Instance.UpdateFriendShip(theName, mercenaryName);
    //    }
    //    else
    //    {
    //        InstanceUIViewManager.Instance.HideFriendShip();
    //    }
    //}


    // 通关奖励界面点击确定
    void OnPassRewardUIOKUp()
    {
        InstanceUIViewManager.Instance.ResetIsShowingPassUIOrRewardUI();
        // InstanceUIViewManager.Instance.ShowInstancePassRewardUI(false);
        EventDispatcher.TriggerEvent(Events.InstanceEvent.WinReturnHome);
    }

    #endregion


    #region 任务打开副本界面

    /// <summary>
    /// 副本界面的打开方式类型
    /// </summary>
    private MissionOpenType m_currentMissionOpenType = MissionOpenType.Quest;
    public MissionOpenType CurrentMissionOpenType
    {
        get { return m_currentMissionOpenType; }
        set
        {
            m_currentMissionOpenType = value;
        }
    }

    /// <summary>
    /// 获取悬浮提示文字ID 
    /// </summary>
    /// <returns></returns>
    private int GetTipTextID()
    {
        if (CurrentMissionOpenType == MissionOpenType.Quest)
            return 46979;
        else if (CurrentMissionOpenType == MissionOpenType.Drop)
            return 46980;
        else if (CurrentMissionOpenType == MissionOpenType.LevelUpgradeGuide)
            return 48509;

        return 0;
    }

    // 任务打开副本界面：设置地图，设置关卡，设置难度任务提示显示
    public void MissionOpen(int theMission, int theLevel)
    {
        MissionOpenAllTheWay(theMission, theLevel, MissionOpenType.Quest);
    }

    // 其他方式打开副本：不限任务，包括副本掉落
    private int MissionOpenMission = 0;
    private int MissionOpenLevel = 0; // 普通 1; 困难2；
    public void MissionOpenAllTheWay(int theMission, int theLevel, MissionOpenType type = MissionOpenType.Quest)
    {
        InstanceMissionChooseUIViewManager.Instance.ShowMissionFoggyAbyssGridTip(false);

        // 如果当前任务无法等级不足，则替换成"升级推荐"
        switch (type)
        {
            case MissionOpenType.Quest:
                if (!MogoWorld.thePlayer.IsMissionCanEnter(theMission, theLevel))
                {
                    KeyValuePair<int, int> theLastMission = MogoWorld.thePlayer.GetLastMissionCanEnter();
                    theMission = theLastMission.Key;
                    theLevel = theLastMission.Value;
                    type = MissionOpenType.LevelUpgradeGuide;
                }
                break;

            case MissionOpenType.FoggyAbyss:
                SetMapID(InstanceMissionChooseUIViewManager.Instance.MissionFoggyAbyssMapID);
                MogoUIManager.Instance.ShowInstanceMissionChooseUI();
                InstanceMissionChooseUIViewManager.Instance.ShowMissionFoggyAbyssGridTip(true);
                return;
        }

        MissionOpenMission = theMission;
        MissionOpenLevel = theLevel;

        CurrentMissionOpenType = type;
        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            foreach (var data in MapUIMappingData.dataMap)
            {
                if (data.Value.grid.ContainsValue(theMission))
                {
                    SetMapID(data.Key);
                    foreach (var item in data.Value.grid)
                    {
                        if (item.Value == theMission)
                        {
                            MogoUIManager.Instance.ShowInstanceMissionChooseUI();
                            InstanceMissionChooseUIViewManager.Instance.ShowGridTip(data.Key, item.Key, true, GetTipTextID());   

                            taskOpenMissionGrid = item.Key;
                            break;
                        }
                    }
                    break;
                }
            }
        }

            // Old Code
        else
        {
            foreach (var data in MapUIMappingData.dataMap)
            {
                if (data.Value.grid.ContainsValue(theMission))
                {
                    SetMapID(data.Key);
                    foreach (var item in data.Value.grid)
                    {
                        if (item.Value == theMission)
                        {
                            MogoUIManager.Instance.ShowNewInstanceChooseMissionUI();
                            NewInstanceUIChooseLevelViewManager.Instance.ShowGridTip(item.Key, true);
                            taskOpenMissionGrid = item.Key;
                            break;
                        }
                    }
                    break;
                }
            }
        }      
    }

    #endregion


    #region 副本扫荡
    /// <summary>
    /// 扫荡流程
    /// 1.请求服务器，如果可以成功扫荡，则进行下一步
    /// 2.获取该关卡的怪物信息以及物品信息，播放怪物信息
    /// 3.播放完后怪物信息后，真正请求扫荡，同时显示物品奖励
    /// 4.点击领取奖励关闭扫荡界面(点击领取奖励后才提示获取奖励)
    /// </summary>

    // 点击扫荡，请求怪物列表和奖励信息
    void OnCleanUp()
    {
        if (InstanceCleanUIViewManager.Instance.IsCleaning)
        {
            InstanceCleanUIViewManager.Instance.IsCleaning = false;
            InstanceCleanUIViewManager.Instance.StopPlayAnimationOutSide();
        }
        else
        {
            EventDispatcher.TriggerEvent<int, int>(Events.InstanceEvent.GetSweepMissionList, GetMissionId(gridID), levelID);
        }
    }

    /// <summary>
    /// 怪物击杀播放完毕，真正请求扫荡
    /// </summary>
    void OnCleanRealUp()
    {
        EventDispatcher.TriggerEvent<int, int>(Events.InstanceEvent.SweepMission, GetMissionId(gridID), levelID);
    }

    /// <summary>
    /// 关闭扫荡界面
    /// </summary>
    void OnCleanUICloseUp()
    {
        MogoUIManager.Instance.LoadMogoInstanceCleanUI(false);
        MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);
    }

    /// <summary>
    /// 按下领取奖励按钮，关闭扫荡界面
    /// </summary>
    void OnRewadUp()
    {        
        OnCleanUICloseUp();// 领取奖励则关闭扫荡界面
    }

    /// <summary>
    /// 显示怪物击杀
    /// </summary>
    /// <param name="data"></param>
    public void OpenMonsterReport(SweepMissionRepostData data)
    {
        InstanceCleanUIViewManager.Instance.IsCleaning = true;
        InstanceCleanUIViewManager.Instance.GenerateMonsterReport(data);
    }

    #endregion


    #region 副本查看任务

    public void OnInstanceShowTask()
    {
        if (MogoWorld.thePlayer == null)
            return;

        if (MogoWorld.thePlayer.CurrentTask != null
            && MogoWorld.thePlayer.CurrentTask.conditionType == 1
            && MogoWorld.thePlayer.CurrentTask.condition.Count > 2)
        {
            int traceTaskID = MogoWorld.thePlayer.CurrentTask.condition[2];
            if (!TaskData.dataMap.ContainsKey(traceTaskID))
                return;

            TaskData traceTask = TaskData.dataMap[traceTaskID];
            InstanceTaskRewardUIViewManager.Instance.SetExpNum(traceTask.exp);
            InstanceTaskRewardUIViewManager.Instance.SetGoldNum(traceTask.money);

            List<string> traceTaskItemImage = new List<string>();
            List<int> traceTaskItemID = new List<int>();
            List<int> traceTaskItemNum = new List<int>();
            Dictionary<int, int> traceTaskItem = new Dictionary<int, int>();
            switch (MogoWorld.thePlayer.vocation)
            {
                case Mogo.Game.Vocation.Warrior:
                    traceTaskItem = traceTask.awards1;
                    break;

                case Mogo.Game.Vocation.Assassin:
                    traceTaskItem = traceTask.awards2;
                    break;

                case Mogo.Game.Vocation.Archer:
                    traceTaskItem = traceTask.awards3;
                    break;

                case Mogo.Game.Vocation.Mage:
                    traceTaskItem = traceTask.awards4;
                    break;
            }

            if (traceTaskItem != null)
            {
                foreach (var traceTaskItemData in traceTaskItem)
                {
                    ItemParentData tempItemData = ItemParentData.GetItem(traceTaskItemData.Key);
                    if (tempItemData != null)
                    {
                        traceTaskItemImage.Add(tempItemData.Icon);
                        traceTaskItemID.Add(traceTaskItemData.Key);
                        traceTaskItemNum.Add(traceTaskItemData.Value);
                    }
                }
            }

            InstanceTaskRewardUIViewManager.Instance.SetRewardItemID(traceTaskItemID, traceTaskItemNum);

            // InstanceTaskRewardUIViewManager.Instance.SetInstanceLevelRewardItemImage(traceTaskItemImage);
            // InstanceTaskRewardUIViewManager.Instance.SetInstanceLevelRewardItemCount(traceTaskItemNum);

            MogoUIManager.Instance.LoadMogoInstanceTaskRewardUI(true);
        }
    }

    public void OnCloseInstanceTask()
    {
        MogoUIManager.Instance.LoadMogoInstanceTaskRewardUI(false);
        MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);
    }

    #endregion


    #region 副本宝箱

    // 点击地图的宝箱
    void OnMapChestUp()
    {
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateChestMessage);
        MogoUIManager.Instance.LoadMogoInstanceTreasureChestUI(true);
    }  

    // 点击宝箱的确定按钮
    void OnMapChestConfirmUp()
    {
        InstanceUIViewManager.Instance.ShowChestWindow(false);
    }


    void OnMapChestGetUp(int id)
    {
        EventDispatcher.TriggerEvent(Events.InstanceEvent.GetChestReward, id);
    }


    void OnMapChestOkUp()
    {
        MogoUIManager.Instance.LoadMogoInstanceTreasureChestUI(false);

        if (InstanceMissionChooseUIViewManager.SHOW_MISSION_BY_DRAG)
        {
            MogoUIManager.Instance.ShowInstanceMissionChooseUI(true);
        }
        else
        {
            MogoUIManager.Instance.ShowNewInstanceChooseMissionUI(true);
        }        
    }

    #endregion


    #region 钻石重置

    // 点击高难度的钻石：
    void GetResetTimes()
    {
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.CheckMissionTimes);
    }


    protected void OnResetMissionUp()
    {
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.ShowResetMissionWindow, GetMissionId(gridID), levelID);
    }


    // 显示钻石重置框
    public void ShowResetMissionWindow(bool isShow, int mode, int cost = 0, int resetTimes = 0)
    {
        if (isShow)
        {
            // string text = "";
            switch (mode)
            {
                case 0:
                    //InstanceUIViewManager.Instance.SetCanResetMissionWindowTextNum(arg.ToString());
                    //InstanceUIViewManager.Instance.ShowResetMissionWindow(true, true);
                    
                    //InstanceLevelChooseUIViewManager.Instance.SetCanResetMissionWindowResetCost(cost);
                    //InstanceLevelChooseUIViewManager.Instance.SetCanResetMissionWindowResetLastTimes(resetTimes);
                    InstanceLevelChooseUIViewManager.Instance.SetCanResetMissionWindowCostAndTimes(cost, resetTimes);
                    InstanceLevelChooseUIViewManager.Instance.ShowCanResetMissionWindow(true);
                    break;

                case 1:
                    //text = LanguageData.dataMap[300000].content;
                    //InstanceUIViewManager.Instance.SetCanNotResetMissionWindowText(text);
                    //InstanceUIViewManager.Instance.ShowResetMissionWindow(true, false);

                    InstanceLevelChooseUIViewManager.Instance.SetCanNotResetMissionWindowText(LanguageData.dataMap[25556].content);
                    InstanceLevelChooseUIViewManager.Instance.ShowCanNotResetMissionWindow(true);
                    break;

                case 2:
                    //text = LanguageData.dataMap[300001].content + arg.ToString() + LanguageData.dataMap[300002].content + MogoWorld.thePlayer.diamond + LanguageData.dataMap[300003].content;
                    //InstanceUIViewManager.Instance.SetCanNotResetMissionWindowText(text);
                    //InstanceUIViewManager.Instance.ShowResetMissionWindow(true, false);

                    //InstanceLevelChooseUIViewManager.Instance.SetCanResetMissionWindowResetCost(cost);
                    //InstanceLevelChooseUIViewManager.Instance.SetCanResetMissionWindowResetLastTimes(resetTimes);
                    InstanceLevelChooseUIViewManager.Instance.SetCanResetMissionWindowCostAndTimes(cost, resetTimes);
                    InstanceLevelChooseUIViewManager.Instance.ShowCanResetMissionWindow(true);
                    break;

                case 3:
                    InstanceLevelChooseUIViewManager.Instance.SetCanNotResetMissionWindowText(LanguageData.dataMap[25557].content);
                    InstanceLevelChooseUIViewManager.Instance.ShowCanNotResetMissionWindow(true);
                    break;
            }
        }
        else
        {
            InstanceUIViewManager.Instance.ShowResetMissionWindow(false, false);
        }
    }


    // 点击请求重置副本次数
    void OnCanResetMissionConfirmUp()
    {
        EventDispatcher.TriggerEvent(Events.InstanceEvent.ResetMission);
        InstanceUIViewManager.Instance.ShowResetMissionWindow(false, false);
    }


    // 取消重置副本
    void OnCanResetMissionCancelUp()
    {
        InstanceUIViewManager.Instance.ShowResetMissionWindow(false, false);
    }


    // 不重置副本
    void OnCanNotResetMissionConfirmUp()
    {
        InstanceUIViewManager.Instance.ShowResetMissionWindow(false, false);
    }

    #endregion


    #region 副本关卡

    void OnMapPageMoveDone(int page)
    {
        if (mapID != page)
        {
            SetMapID(page);
            EventDispatcher.TriggerEvent(Events.InstanceUIEvent.UpdateChestMessage);
        }
    }

    /// <summary>
    /// 点击地图的Boss宝箱
    /// </summary>
    void OnBossChestButtonUp()
    {
        MogoUIManager.Instance.ShowInstanceBossTreasureUI(null);
    }

    /// <summary>
    /// 点击进入随机副本
    /// </summary>
    /// <param name="i"></param>
    void OnMissionRandomEnterUp()
    {
        LoggerHelper.Debug("OnMissionRandomEnterUp");
        EventDispatcher.TriggerEvent(Events.InstanceEvent.EnterRandomMission);
    }

    #endregion


    #region 新通关界面

    public void SetNewPassMessage(int minutes, int second, int maxCombo, int scorePoint, int starNum)
    {
        BattlePassUILogicManager.Instance.SetPassTime(string.Concat(minutes.ToString("d2"), " : ", second.ToString("d2")));
        BattlePassUILogicManager.Instance.SetComboNum(maxCombo.ToString());
        BattlePassUILogicManager.Instance.SetSocre(scorePoint.ToString());
        BattlePassUILogicManager.Instance.SetGrade(starNum);

        BattlePassCardListUILogicManager.Instance.SetGrade(starNum);
    }

    public void SetNewInstanceRewardUIReward(List<int> ids, List<int> counts)
    {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < ids.Count; i++)
        {
            if (i < counts.Count)
            {
                sb.Append(ItemParentData.GetNameWithNum(ids[i], counts[i]));
                sb.Append(" ");
            }
        }
        BattlePassUILogicManager.Instance.SetRewardList(sb.ToString());
        BattlePassUILogicManager.Instance.PlayScroeAnim();
    }

    #endregion


    #region 迷雾深渊

    public int foggyAbyssCurrentState = 0;
    public int foggyAbyssCurrentMissionID = 0;
    public int foggyAbyssCurrentMissionDifficulty = 0;
    public bool foggyAbyssCurrentMissionHasPlay = false;

    public void FoggyAbyssTipEnter()
    {
        KeyValuePair<int, int> pair = MogoWorld.thePlayer.GetLastMissionCanEnter();
        InstanceUILogicManager.Instance.MissionOpenAllTheWay(pair.Key, pair.Value, MissionOpenType.FoggyAbyss);
    }

    public void GetFoggyAbyssMessage()
    {
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetFoggyAbyssMessage);
    }

    public void SetSpecialMissionMessage(int state, int missionID, int level, bool missionHasPlay)
    {
        foggyAbyssCurrentState = state;
        foggyAbyssCurrentMissionID = missionID;
        foggyAbyssCurrentMissionDifficulty = level;
        foggyAbyssCurrentMissionHasPlay = missionHasPlay;
    }

    public void OnChooseFoggyAbyssGridUp()
    {
        foreach (var mData in MapUIMappingData.dataMap)
            InstanceMissionChooseUIViewManager.Instance.HideGridTip(mData.Key);

        MogoUIManager.Instance.LoadMogoInstanceLevelChooseUI(true);
        InstanceLevelChooseUIViewManager.Instance.ChangeMode(MissionType.FoggyAbyss);

        // title
        InstanceLevelChooseUIViewManager.Instance.SetInstanceChooseGridTitle(LanguageData.GetContent(48513), foggyAbyssCurrentState);

        if (foggyAbyssCurrentState == 3 && foggyAbyssCurrentMissionHasPlay)
        {
            InstanceLevelChooseUIViewManager.Instance.ShowStars(4);
            InstanceLevelChooseUIViewManager.Instance.SetEnterTimes(MissionType.FoggyAbyss, 0, 0, true);
        }
        else
        {
            InstanceLevelChooseUIViewManager.Instance.ShowStars(0);
            InstanceLevelChooseUIViewManager.Instance.SetEnterTimes(MissionType.FoggyAbyss, 0, 0, false);
        }

        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.GetDrops, foggyAbyssCurrentMissionID);

        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.ShowCard, foggyAbyssCurrentMissionID, foggyAbyssCurrentMissionDifficulty);

        levelID = foggyAbyssCurrentMissionDifficulty;
    }

    // 点击进入[特殊副本-迷雾深渊]
    void OnEnterFoggyAbyssUp()
    {
        EventDispatcher.TriggerEvent(Events.InstanceUIEvent.EnterFoggyAbyss, foggyAbyssCurrentMissionID, foggyAbyssCurrentMissionDifficulty);
    }

    #endregion

}