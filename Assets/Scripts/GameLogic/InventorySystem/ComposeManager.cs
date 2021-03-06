/*----------------------------------------------------------------
// Copyright (C) 2013 广州，爱游
//
// 模块名：ComposeManager
// 创建者：Joe Mo
// 修改者列表：
// 创建日期：2013-4-16
// 模块描述：宝石合成系统
//----------------------------------------------------------------*/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Mogo.GameData;
using Mogo.Util;
using Mogo.Game;

public class ComposeManager
{
    public const int JEWEL_NAME_START_ID = 177;
    public const string ON_COMPOSE = "ComposeManager.ON_COMPOSE";
    public const string ON_COMPOSE_NOW = "ComposeManager.ON_COMPOSE_NOW";
    public const string ON_COMPOSE_SHOW = "ComposeManager.ON_COMPOSE_SHOW";
    public const string ON_JEWEL_SELECT = "ComposeManager.ON_JEWEL_SELECT";
    public const string ON_JEWEL_TYPE_SELECT = "ComposeManager.ON_JEWEL_TYPE_SELECT";
    public const string ON_BUY = "ComposeManager.ON_BUY";

    InventoryManager m_inventoryManager;
    //跟背包中宝石数据同步，用与合成宝石界面逻辑,<subtype,<level,count>>
    private Dictionary<int, Dictionary<int, int>> m_jewelDic = new Dictionary<int, Dictionary<int, int>>();
    //<templateId>
    private List<int> m_jewelCanComposeList = new List<int>();
    private ItemJewelData m_currentJewel;
    private int m_currentParentId;
    private int m_currentChildId;
    int m_versionID;
    static public ComposeManager Instance;

    public ComposeManager(InventoryManager inventoryManager)
    {
        m_inventoryManager = inventoryManager;
        AddListener();
        m_versionID = inventoryManager.m_versionId;
        Instance = this;
    }

    private void AddListener()
    {
        EventDispatcher.AddEventListener<int, int>(ON_JEWEL_SELECT, OnJewelSelect);
        EventDispatcher.AddEventListener<int>(ON_JEWEL_TYPE_SELECT, OnJewelTypeSelect);
        EventDispatcher.AddEventListener(ON_COMPOSE, OnCompose);
        EventDispatcher.AddEventListener(ON_COMPOSE_NOW, OnComposeNow);
        EventDispatcher.AddEventListener(ON_COMPOSE_SHOW, OnComposeShow);
        EventDispatcher.AddEventListener(ON_BUY, OnBuy);
        EventDispatcher.AddEventListener<int, int>(Events.ComposeManagerEvent.SwitchToCompose, SwitchToCompose);
    }

    private void OnBuy()
    {
        Action act = () => { SwitchToCompose(m_currentParentId, m_currentChildId); };
        EventDispatcher.TriggerEvent(MarketEvent.OpenWithJewel, act);
    }

    public void RemoveListener()
    {
        EventDispatcher.RemoveEventListener<int, int>(ON_JEWEL_SELECT, OnJewelSelect);
        EventDispatcher.RemoveEventListener<int>(ON_JEWEL_TYPE_SELECT, OnJewelTypeSelect);
        EventDispatcher.RemoveEventListener(ON_COMPOSE, OnCompose);
        EventDispatcher.RemoveEventListener(ON_COMPOSE_NOW, OnComposeNow);
        EventDispatcher.RemoveEventListener(ON_COMPOSE_SHOW, OnComposeShow);
        EventDispatcher.RemoveEventListener(ON_BUY, OnBuy);
        EventDispatcher.RemoveEventListener<int, int>(Events.ComposeManagerEvent.SwitchToCompose, SwitchToCompose);
    }

    private void OnComposeNow()
    {
        LoggerHelper.Debug("OnComposeNow?");
        LoggerHelper.Debug("OnComposeNow,level:" + (m_currentJewel.level + 1) + ",subtype:" + m_currentJewel.subtype);

        int level = (m_currentJewel.level + 1);

        MogoWorld.thePlayer.RpcCall("JewelCombineAnywayMoneyReq", (byte)m_currentJewel.subtype, (byte)level);
    }

    private void OnDoComposeNow()
    {
        MogoWorld.thePlayer.RpcCall("JewelCombineAnywayReq", (byte)m_currentJewel.subtype, (byte)(m_currentJewel.level + 1));
    }

    /// <summary>
    /// m_jewelDic根据m_itemsInBag[Item_Jewel]更新
    /// </summary>
    private void UpdateJewelDic()
    {
        m_jewelDic.Clear();
        foreach (ItemParent item in m_inventoryManager.JewelInBag.Values)
        {
            ItemJewel jewel = (ItemJewel)item;

            if (!m_jewelDic.ContainsKey(jewel.subtype))
                m_jewelDic.Add(jewel.subtype, new Dictionary<int, int>());
            if (!m_jewelDic[jewel.subtype].ContainsKey(jewel.level))
                m_jewelDic[jewel.subtype][jewel.level] = 0;
            m_jewelDic[jewel.subtype][jewel.level] += jewel.stack;
        }
    }

    bool m_isSwitchFromBag = false;
    private bool m_isRefreshing = false;
    private bool m_isNeedRefresh = false;
    private bool m_isComposing;
    private uint m_timerId;
    private void OnComposeShow()
    {
        //Debug.LogError("OnComposeShow()");
        if (!m_isSwitchFromBag)
        {
            m_currentJewel = null;//在refreshUI里判断但此为null时会重新计算pId,cId
        }
        else
        {
            m_isSwitchFromBag = false;
        }
        RefreshUI();
    }

    public void RefreshUI()
    {
        //if (m_versionID >= m_inventoryManager.m_versionId) return;
        if (ComposeUIViewManager.Instance == null) return;

        if (m_isRefreshing)
        {
            //Debug.LogError("Debug.LogError(m_isRefreshing);");
            m_isNeedRefresh = true;
            return;
        }
        m_isRefreshing = true;
        Mogo.Util.LoggerHelper.Debug("RefreshUI");
        UpdateJewelDic();
        Dictionary<int, LanguageData> nameData = LanguageData.dataMap;
        ComposeUIViewManager view = ComposeUIViewManager.Instance;
        Dictionary<int, Dictionary<int, ItemJewelData>> jewelDB = ItemJewelData.GetJewelDic();
        view.RemoveIconList();

        PrivilegeData privilegeData = PrivilegeData.dataMap[MogoWorld.thePlayer.VipLevel];

        //可合成宝石

        int count = 0;
        m_jewelCanComposeList.Clear();

        List<string> canComposeStrList = new List<string>();
        for (int subtype = 1; subtype <= ItemJewelData.TypeNum; subtype++)
        {
            if (!m_jewelDic.ContainsKey(subtype)) continue;
            for (int level = privilegeData.jewelSynthesisMaxLevel - 1; level >= 1; level--)
            {
                if (!m_jewelDic[subtype].ContainsKey(level)) continue;

                if (m_jewelDic[subtype][level] > 2)
                {
                    canComposeStrList.Add(string.Concat("[FFFFFF]", jewelDB[subtype][level + 1].NameWithoutColor, "[-]"));
                    count++;
                    m_jewelCanComposeList.Add(jewelDB[subtype][level].id);
                }
            }
        }

        //计算要加载的条数，用于异步加载
        view.IconGridNum = ItemJewelData.TypeNum + 1;
        view.IconChildGridNum = (privilegeData.jewelSynthesisMaxLevel - 1) * (ItemJewelData.TypeNum) + canComposeStrList.Count;
        view.AddIconListGrid(nameData[JEWEL_NAME_START_ID].content, 0);

        for (int i = 0; i < canComposeStrList.Count; i++)
        {
            view.AddIconListGridChild(0, i, canComposeStrList[i]);
        }


        //核心宝石
        //int coreJewelType = 1;

        //string coreJewelName = nameData[JEWEL_NAME_START_ID + coreJewelType].content;
        //view.AddIconListGrid(coreJewelName, 1);
        //for (int j = 0; j < 8; ++j)
        //{
        //    int level = j + 2;
        //    if (level > privilegeData.jewelSynthesisMaxLevel) break;
        //    view.AddIconListGridChild(1, j, jewelDB[coreJewelType][j + 2].Name);
        //}

        //其他宝石
        Mogo.Util.LoggerHelper.Debug("ItemJewelData.MaxType:" + ItemJewelData.MaxType);

        for (int i = 0; i < ItemJewelData.TypeNum; ++i)
        {
            int type = i + 1;
            if (!jewelDB.ContainsKey(type)) continue;

            view.AddIconListGrid(nameData[JEWEL_NAME_START_ID + type].content, type);

            for (int j = 0; j < ItemJewelData.JewelLevelNum - 1; ++j)
            {
                int level = j + 2;
                if (level > privilegeData.jewelSynthesisMaxLevel) break;
                string name = jewelDB[type][level].NameWithoutColor;
                //Mogo.Util.LoggerHelper.Debug(name);
                if (m_jewelDic.ContainsKey(type))
                {
                    if (m_jewelDic[type].ContainsKey(level - 1))
                    {
                        if (m_jewelDic[type][level - 1] > 2)
                        {
                            name = string.Concat("[FFFFFF]", name, "[-]");
                        }
                    }
                }
                //Mogo.Util.LoggerHelper.Debug("type:" + type + ",j:" + j + ",name:" + name);
                view.AddIconListGridChild(type, j, name);
            }
        }

        view.SetComposePearlLeftIcon(IconData.none);
        view.SetComposePearlRightIcon(IconData.none);
        view.SetComposePearlUpIcon(IconData.none);

        view.RepositionNow();


        if (m_currentJewel == null)
        {
            if (m_jewelCanComposeList.Count > 0)
            {
                Mogo.Util.LoggerHelper.Debug("(0, 0)");
                m_currentParentId = 0;
                m_currentChildId = 0;
            }
            else
            {
                Mogo.Util.LoggerHelper.Debug("(1, 0)");
                m_currentParentId = 1;
                m_currentChildId = 0;
            }
        }

        Mogo.Util.LoggerHelper.Debug("m_currentParentId:" + m_currentParentId + ",m_currentChildId:" + m_currentChildId);
        OnJewelSelect(m_currentParentId, m_currentChildId);
        m_versionID = m_inventoryManager.m_versionId;
        Mogo.Util.LoggerHelper.Debug("m_currentParentId:" + m_currentParentId + ",m_currentChildId:" + m_currentChildId);


        MogoUIManager.Instance.SwitchComposeUI(delegate()
        {
            //Debug.LogError("SwitchComposeUI done");
            view.SetCurrentGridDown(m_currentParentId, m_currentChildId);
            m_isRefreshing = false;
            if (m_isNeedRefresh)
            {
                //Debug.LogError("m_isNeedRefresh");
                m_isNeedRefresh = false;
                RefreshUI();
            }
            //TimerHeap.AddTimer(400, 0, delegate()
            //{
            //    view.SetCurrentGridDown(m_currentParentId, m_currentChildId);
            //});
        }, false);
        //TimerHeap.AddTimer(500, 0, delegate()
        //{
        //    view.SetCurrentGridDown(m_currentParentId, m_currentChildId);
        //});
        Mogo.Util.LoggerHelper.Debug("refreshUi done");

    }

    private void OnJewelSelect(int parentId, int id)
    {
        Mogo.Util.LoggerHelper.Debug("parentId:" + parentId + ",id:" + id);
        ComposeUIViewManager view = ComposeUIViewManager.Instance;
        m_currentParentId = parentId;
        m_currentChildId = id;
        view.SetComposeNowBtnEnable(true);
        view.SetComposeBtnEnable(true);
        if (parentId == 0 && m_jewelCanComposeList.Count > id)
        {
            ItemJewelData data = ItemJewelData.dataMap[m_jewelCanComposeList[id]];
            ItemJewelData composeJewel = ItemJewelData.GetJewelDic()[data.subtype][data.level + 1];
            view.SetComposePearlLeftIcon(data.Icon, data.color, data.id);
            view.SetComposePearlRightIcon(data.Icon, data.color, data.id);
            view.SetComposePearlUpIcon(data.Icon, data.color, data.id);
            view.SetComposePearlFinalIcon(composeJewel.Icon, composeJewel.color, composeJewel.id);
            m_currentJewel = data;
            //Mogo.Util.LoggerHelper.Debug("m_currentParentId:" + m_currentParentId + ",m_currentChildId:" + m_currentChildId);
            view.SetComposeNowBtnEnable(false);
            LoggerHelper.Debug("m_currentJewel:" + m_currentJewel.subtype + ",level:" + m_currentJewel.level);
            return;
        }
        else if (parentId == 0)
        {
            m_currentParentId = 1;
            parentId = 1;
            m_currentChildId = 0;
            id = 0;
        }
        Mogo.Util.LoggerHelper.Debug("m_currentParentId:" + m_currentParentId + ",m_currentChildId:" + m_currentChildId);
        view.SetComposePearlLeftIcon(IconData.none);
        view.SetComposePearlRightIcon(IconData.none);
        view.SetComposePearlUpIcon(IconData.none);

        int type = parentId;
        int level = id + 1;

        m_currentJewel = ItemJewelData.GetJewelDic()[type][level];


        ItemJewelData jewel = ItemJewelData.JewelDic[type][level];
        ItemJewelData nextJewel = ItemJewelData.JewelDic[type][level + 1];
        Mogo.Util.LoggerHelper.Debug("nextJewel:" + nextJewel.Name);
        view.SetComposePearlFinalIcon(nextJewel.Icon, nextJewel.color, nextJewel.id);
        view.SetComposePearlUpIcon(jewel.Icon);
        view.SetComposePearlLeftIcon(jewel.Icon);
        view.SetComposePearlRightIcon(jewel.Icon);
        if (!m_jewelDic.ContainsKey(type))
        {
            view.SetComposeBtnEnable(false);
            return;
        }
        if (!m_jewelDic[type].ContainsKey(level))
        {
            view.SetComposeBtnEnable(false);
            return;
        }

        int num = m_jewelDic[type][level];

        view.SetComposeBtnEnable(false);
        if (num >= 1) view.SetComposePearlUpIcon(jewel.Icon, jewel.color, jewel.id);
        if (num >= 2) view.SetComposePearlLeftIcon(jewel.Icon, jewel.color, jewel.id);
        if (num >= 3)
        {
            view.SetComposePearlRightIcon(jewel.Icon, jewel.color, jewel.id);
            view.SetComposeNowBtnEnable(false);
            view.SetComposeBtnEnable(true);
        }
    }

    private void OnJewelTypeSelect(int parentId)
    {
        // 可合成宝石选项
        if (parentId == 0 && m_jewelCanComposeList.Count == 0)
        {
            MogoMsgBox.Instance.ShowFloatingText(LanguageData.GetContent(48008)); // 没有可合成的宝石
        }
    }

    private void OnCompose()
    {
        LoggerHelper.Debug("OnCompose");
        if (m_currentJewel == null) return;
        Compose(m_currentJewel.subtype, m_currentJewel.level);
        LoggerHelper.Debug(m_currentJewel.subtype + "," + m_currentJewel.level);
    }

    public void Compose(int type, int level)
    {
        LoggerHelper.Debug("Compose");
        LoggerHelper.Debug("(byte)type:" + type + ", (byte)level:" + level);
        if (m_isComposing) return;
        m_isComposing = true;
        m_timerId = TimerHeap.AddTimer(2000, 0, () => { m_isComposing = false; });

        MogoWorld.thePlayer.RpcCall("JewelCombineReq", (byte)type, (byte)(level + 1));
    }

    public void SwitchToCompose(int parentId, int childId)
    {
        InventoryManager.Instance.CurrentView = InventoryManager.View.ComposeView;

        InventoryManager.Instance.m_currentEquipmentView = InventoryManager.View.ComposeView;
        EquipTipManager.Instance.CloseEquipTip();
        if (parentId > 0)
        {
            m_currentJewel = ItemJewelData.JewelDic[parentId][childId + 2];
            m_currentParentId = parentId;
            m_currentChildId = childId;
            m_isSwitchFromBag = true;
        }

        MogoUIManager.Instance.SwitchComposeUI();//() => { OnSwitchComposeUIDone(subType, level); }
    }

    public void JewelCombineResp(byte subType, byte level, byte errorId)
    {
        m_isComposing = false;
        TimerHeap.DelTimer(m_timerId);
        int type = (int)subType;
        //int level = m_currentJewel.level;
        Mogo.Util.LoggerHelper.Debug("JewelCombineResp:" + errorId);
        switch (errorId)
        {
            case 0:
                MogoGlobleUIManager.Instance.ShowComposeSucessSign(true);
                if (ComposeUIViewManager.Instance != null && InventoryManager.Instance.CurrentView == InventoryManager.View.ComposeView)
                    ComposeUIViewManager.Instance.PlayUIFXAnim();
                if (MogoUIManager.Instance.CurrentUI == MogoUIManager.Instance.m_ComposeUI)
                    RefreshUI();
                break;
            case 4:
                Mogo.Util.LoggerHelper.Debug("JewelCombineResp:" + errorId);
                //string msg = LanguageData.dataMap[425].Format(ItemJewelData.JewelDic[type][level - 1].Name);
                //MogoMsgBox.Instance.ShowMsgBox(msg);
                //在背包点合成时候转到合成界面，合成界面无法点合成所以不会有这个返回

                SwitchToCompose(subType, level - 2);
                //InventoryManager.Instance.CurrentView = InventoryManager.View.ComposeView;

                //InventoryManager.Instance.m_currentEquipmentView = InventoryManager.View.ComposeView;
                //EquipTipManager.Instance.CloseEquipTip();
                //m_currentJewel = ItemJewelData.JewelDic[subType][level];
                //m_currentParentId = subType;
                //m_currentChildId = level - 2;
                //m_isSwitchFromBag = true;
                //MogoUIManager.Instance.SwitchComposeUI();//() => { OnSwitchComposeUIDone(subType, level); }

                break;
            default:
                Mogo.Util.LoggerHelper.Debug("JewelCombineResp:" + errorId);
                Mogo.Util.LoggerHelper.Debug("JewelCombineResp:" + type + "," + level);
                if (ItemJewelData.GetJewelDic()[type][level] == null) Mogo.Util.LoggerHelper.Debug("fuck!");

                else
                {
                    InsetManager.ShowInfoByErrorId(errorId, ItemJewelData.GetJewelDic()[type][level]);
                }
                break;
        }

        Mogo.Util.LoggerHelper.Debug("JewelCombineResp:" + errorId);
        EquipTipManager.Instance.CloseEquipTip();

    }

    private void OnSwitchComposeUIDone(int subType, int level)
    {
        Mogo.Util.LoggerHelper.Debug("OnSwitchComposeUIDone(int subType, int level):" + subType + "," + level);
        m_currentJewel = ItemJewelData.JewelDic[subType][level];
        m_currentParentId = subType;
        m_currentChildId = level - 2;
        OnJewelSelect(m_currentParentId, m_currentChildId);

    }

    public void JewelCombineInEqiResp(byte errorId)
    {
        int type = m_currentJewel.subtype;
        int level = m_currentJewel.level + 1;
        InsetManager.ShowInfoByErrorId(errorId, ItemJewelData.JewelDic[type][level]);
        LoggerHelper.Debug("JewelCombineInEqiResp:" + errorId);
    }

    public void JewelCombineAnywayMoneyResp(uint money)
    {
        Mogo.Util.LoggerHelper.Debug("JewelCombineAnywayMoneyResp:" + money);
        string msg = "";
        if (money > 0)
        {
            msg = LanguageData.dataMap[440].Format(money);
        }
        else
        {
            msg = LanguageData.dataMap[439].content;

        }
        MogoGlobleUIManager.Instance.Confirm(msg, (rst) =>
            {
                if (rst)
                {
                    OnDoComposeNow();
                    MogoGlobleUIManager.Instance.ConfirmHide();
                }
                else
                {
                    MogoGlobleUIManager.Instance.ConfirmHide();
                }
            }
       );
    }

    public void JewelCombineAnywayResp(byte errorId)
    {
        int type = m_currentJewel.subtype;
        int level = m_currentJewel.level + 1;
        JewelCombineResp((byte)type, (byte)level, errorId);
        //InsetManager.ShowInfoByErrorId(errorId, ItemJewelData.JewelDic[type][level]);
        LoggerHelper.Debug("JewelCombineAnywayResp:" + errorId);

    }
}