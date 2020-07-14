﻿//------------------------------------------------------------
// ScrollCircleMaker v1.0
// Copyright © 2020 DaveAnt. All rights reserved.
// Homepage: https://daveant.gitee.io/
// Github: https://github.com/DaveAnt/ScollCircleMaker
//------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UIPlugs.ScrollCircleMaker
{
    public abstract class BaseCircleHelper<T>
    {
        protected List<T> _dataSet;
        protected List<BaseItem<T>> _itemSet;
        protected Func<BaseItem<T>> _createItemFunc;
        protected Action _toLocationEvent;
        protected ScrollRect _scrollRect;
        protected RectTransform _viewRect, _contentRect, _itemRect;
        protected ScrollCircleComponent _sProperty;
        protected GameObject _baseItem;

        /// <summary>
        /// 动画结束回调
        /// </summary>
        public event Action toLocationEvent
        {
            add {
                _toLocationEvent += value;
            }
            remove
            {
                _toLocationEvent -= value;
            }
        }
        /// <summary>
        /// 插件参数
        /// </summary>
        public ScrollCircleComponent sProperty
        {
            get {
                return _sProperty;
            }
        }
        /// <summary>
        /// 视图中心
        /// </summary>
        public int itemCore
        {
            get {
                return ((_sProperty.dataIdx + _sProperty.initItems) / 2) % _dataSet.Count;
            }
        }
        /// <summary>
        /// 数据个数
        /// </summary>
        public int dataCount
        {
            get {
                return _dataSet == null ? 0 : _dataSet.Count;
            }
        }
        /// <summary>
        /// 物品实例数量
        /// </summary>
        public int itemCount
        {
            get {
                return _itemSet == null ? 0 : _itemSet.Count;
            }
        }

        /// <summary>
        /// 滑动构造基类
        /// </summary>
        /// <param name="contentTrans">包含物品父组件</param>
        /// <param name="createItemFunc">创建物品函数</param>
        protected BaseCircleHelper(Transform contentTrans, Func<BaseItem<T>> createItemFunc)
        {
            _createItemFunc = createItemFunc;
            _contentRect = contentTrans as RectTransform;
            _viewRect = contentTrans.parent.GetComponent<RectTransform>();
            _scrollRect = _viewRect.parent.GetComponent<ScrollRect>();
            _sProperty = _contentRect.GetComponent<ScrollCircleComponent>();

            if (_sProperty == null)
                throw new Exception("Content must have ScrollCircleComponent!");
            _baseItem = _sProperty.baseItem;
            _itemRect = _baseItem.transform.GetComponent<RectTransform>();
            _scrollRect.onValueChanged.AddListener(OnRefreshHandler);
            _itemSet = new List<BaseItem<T>>();
            _dataSet = new List<T>();
        }
        /// <summary>
        /// 启动插件
        /// </summary>
        /// <param name="_tmpDataSet"></param>
        public virtual void OnStart(List<T> tmpDataSet = null)
        {
            _firstRun = true;
            lockRefresh = _sProperty.initItems >= _dataSet.Count;
            if (tmpDataSet != null)
            {
                switch (_sProperty.scrollSort)
                {
                    case ScrollSort.BackDir:
                    case ScrollSort.BackZDir:
                        tmpDataSet.Reverse();
                        break;
                }
                _dataSet.AddRange(tmpDataSet);
            }
            for (int i = 0; i < _sProperty.initItems; ++i)
                InitItem(i);
        }
        /// <summary>
        /// 释放插件
        /// </summary>
        public virtual void OnDestroy()
        {
            foreach (var baseItem in _itemSet)
                baseItem.OnDestroy();
            _toLocationEvent = null;
            _createItemFunc = null;
            _scrollRect.onValueChanged.RemoveListener(OnRefreshHandler);
            _dataSet.Clear();
            _itemSet.Clear();
            GC.Collect();
        }
        /// <summary>
        /// 重置插件
        /// </summary>
        public virtual void ResetItems()
        {
            _firstRun = false;
            foreach (var baseItem in _itemSet)
                baseItem.OnDestroy();
            contentSite = (int)topSeat;
            contentRectangle = 0;
            nowSeat = 0;
            _dataSet.Clear();
            _itemSet.Clear();
            GC.Collect();       
        }
        /// <summary>
        /// 物品持续更新
        /// </summary>
        public virtual void OnUpdate()
        {
            if (_itemSet == null) return;
            foreach (BaseItem<T> baseItem in _itemSet)
                baseItem.OnUpdate();
        }
        /// <summary>
        /// 锁定滑动
        /// </summary>
        /// <param name="lockStatus">是否开关</param>
        public virtual void OnSlideLockout(bool lockStatus)
        {
            try {
                _scrollRect.enabled = lockStatus;
            }
            catch (Exception e) {
                Debug.LogError("OnSlideLockout Error!" + e.Message);
            }
        }
        /// <summary>
        /// 刷新方式
        /// </summary>
        /// <param name="v2"></param>
        protected abstract void OnRefreshHandler(Vector2 v2);
        /// <summary>
        /// 移除数据
        /// </summary>
        /// <param name="itemIdx">索引</param>
        public abstract void DelItem(int itemIdx);
        /// <summary>
        /// 移除数据
        /// </summary>
        /// <param name="seekFunc">匹配函数</param>
        /// <param name="data">移除数据</param>
        public abstract void DelItem(Func<T, T, bool> seekFunc, T data);
        /// <summary>
        /// 添加数据
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="itemIdx">索引</param>
        public abstract void AddItem(T data, int itemIdx = int.MaxValue);
        /// <summary>
        /// 初始化物品
        /// </summary>
        /// <param name="itemIdx">位置索引</param>
        protected virtual void InitItem(int itemIdx)
        {
            BaseItem<T> baseItem = _createItemFunc();
            baseItem.gameObject = GameObject.Instantiate(_baseItem, _contentRect);
            baseItem.gameObject.name = _baseItem.name + itemIdx;
            baseItem.InitComponents();
            baseItem.InitEvents();
            _itemSet.Add(baseItem);
        }
        /// <summary>
        /// 更新样式
        /// </summary>
        /// <param name="data">数据</param>
        /// <param name="itemIdx">索引</param>
        public abstract void UpdateItem(T data, int itemIdx);
        /// <summary>
        /// 真实位置定位
        /// </summary>
        /// <param name="toSeat">真实位置</param>
        /// <param name="isDrawEnable">是否存在动画过程</param>
        public abstract void ToLocation(float toSeat, bool isDrawEnable = true);
        /// <summary>
        /// 数据索引定位
        /// </summary>
        /// <param name="toIndex">数据索引</param>
        /// <param name="isDrawEnable">是否存在动画过程</param>
        public abstract void ToLocation(int toIndex, bool isDrawEnable = true);
        /// <summary>
        /// 数据匹配定位
        /// </summary>
        /// <param name="seekFunc">匹配函数</param>
        /// <param name="isDrawEnable">是否存在动画过程</param>
        public abstract void ToLocation(Func<T, T, bool> seekFunc, T data, bool isDrawEnable = true);
        /// <summary>
        /// 置顶
        /// </summary>
        /// <param name="isDrawEnable">是否存在动画过程</param>
        public virtual void ToTop(bool isDrawEnable = true)
        {
            ToLocation(topSeat, isDrawEnable);
        }
        /// <summary>
        /// 置底
        /// </summary>
        /// <param name="isDrawEnable">是否存在动画过程</param>
        public virtual void ToBottom(bool isDrawEnable = true)
        {
            ToLocation(bottomSeat, isDrawEnable);
        }

        #region 辅助器内置共需属性
        /// <summary>
        /// 刷新速率
        /// </summary>
        protected float _timer = 0;
        /// <summary>
        /// 缓存位置
        /// </summary>
        protected Vector2 _cacheSeat;
        /// <summary>
        /// 布局基类
        /// </summary>
        protected LayoutGroup _layoutGroup;
        /// <summary>
        /// 滑动反向
        /// </summary>
        protected sbyte _frontDir = 1;
        /// <summary>
        /// 刷新状态、首次使用
        /// </summary>
        protected bool _lockRefresh = false,_firstRun = false;
        /// <summary>
        /// 禁止刷新
        /// </summary>
        protected bool lockRefresh
        {
            get {
                return _lockRefresh;
            }
            set {
                _lockRefresh = value && !_sProperty.isCircleEnable;
            }
        }
        /// <summary>
        /// 对应高宽
        /// </summary>
        protected float contentRectangle
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        return _contentRect.rect.height;
                    default:
                        return _contentRect.rect.width;
                }
            }
            set
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        _contentRect.sizeDelta = new Vector2(_contentRect.sizeDelta.x, value);
                        break;
                    default:
                        _contentRect.sizeDelta = new Vector2(value, _contentRect.sizeDelta.y);
                        break;
                }
            }
        }
        /// <summary>
        /// 视图自适应高宽
        /// </summary>
        protected float viewRectangle
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        return _viewRect.rect.height;
                    default:
                        return _viewRect.rect.width;
                }
            }
        }
        /// <summary>
        /// 偏移锚点
        /// </summary>
        protected int contentSite
        {
            get
            {
                switch (_sProperty.scrollDir)
                {
                    case ScrollDir.TopToBottom: return _layoutGroup.padding.top;
                    case ScrollDir.BottomToTop: return _layoutGroup.padding.bottom;
                    case ScrollDir.LeftToRight: return _layoutGroup.padding.left;
                    default: return _layoutGroup.padding.right;
                }
            }
            set
            {
                switch (_sProperty.scrollDir)
                {
                    case ScrollDir.TopToBottom: _layoutGroup.padding.top = value; break;
                    case ScrollDir.BottomToTop: _layoutGroup.padding.bottom = value; break;
                    case ScrollDir.LeftToRight: _layoutGroup.padding.left = value; break;
                    default: _layoutGroup.padding.right = value; break;
                }
            }
        }
        /// <summary>
        /// 当前位置
        /// </summary>
        protected float nowSeat
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true: return  _contentRect.anchoredPosition.y * _frontDir;
                    default: return  _contentRect.anchoredPosition.x * _frontDir;
                }
            }
            set
            {
                _cacheSeat = _contentRect.anchoredPosition;
                switch (_scrollRect.vertical)
                {
                    case true: _cacheSeat.y = value * _frontDir; break;
                    default: _cacheSeat.x = value * _frontDir; break;
                }
                _contentRect.anchoredPosition = _cacheSeat; 
            }
        }
        /// <summary>
        /// 数据顶部位置
        /// </summary>
        protected float topSeat
        {
            get
            {
                switch (_sProperty.scrollDir)
                {
                    case ScrollDir.TopToBottom:
                        if (_sProperty.isCircleEnable)
                            return _viewRect.rect.height;
                        return _sProperty.TopExt;
                    case ScrollDir.BottomToTop:
                        if (_sProperty.isCircleEnable)
                            return _viewRect.rect.height;
                        return _sProperty.BottomExt;
                    case ScrollDir.LeftToRight:
                        if (_sProperty.isCircleEnable)
                            return _viewRect.rect.width;
                        return _sProperty.LeftExt;
                    default:
                        if (_sProperty.isCircleEnable)
                            return _viewRect.rect.width;
                        return _sProperty.RightExt;
                }
            }
        }
        /// <summary>
        /// 数据底部位置
        /// </summary>
        protected float bottomSeat
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        if(_sProperty.isCircleEnable)
                            return _contentRect.rect.height - _viewRect.rect.height - _viewRect.rect.height;
                        return _contentRect.rect.height - _viewRect.rect.height;
                    default:
                        if (_sProperty.isCircleEnable)
                            return _contentRect.rect.width - _viewRect.rect.width - _viewRect.rect.width;
                        return _contentRect.rect.width - _viewRect.rect.width;
                }
            }
        }
        /// <summary>
        /// 真实底部位置
        /// </summary>
        protected float footSeat
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        return _contentRect.rect.height - _viewRect.rect.height;
                    default:
                        return _contentRect.rect.width - _viewRect.rect.width;    
                }
            }
        }
        /// <summary>
        /// 扩展边距
        /// </summary>
        protected float contentBorder
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        if (_sProperty.isCircleEnable)
                            return 2 *_viewRect.rect.height;
                        return _sProperty.TopExt + _sProperty.BottomExt;
                    default:
                        if (_sProperty.isCircleEnable)
                            return 2 * _viewRect.rect.width;
                        return _sProperty.LeftExt + _sProperty.RightExt;
                }
            }
        }
        /// <summary>
        /// 物品对应间距
        /// </summary>
        protected int spacingExt
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        return _sProperty.HeightExt;
                    default:
                        return _sProperty.WidthExt;
                }
            }
        }
        /// <summary>
        /// 滑动方向
        /// </summary>
        protected int slideDir
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        if (_scrollRect.velocity.y > 0) return _frontDir;
                        return -_frontDir;
                    default:
                        if (_scrollRect.velocity.x > 0) return _frontDir;
                        return -_frontDir;
                }
            }
        }
        /// <summary>
        /// 上区域判断
        /// </summary>
        protected bool isLowerDefine
        {
            get
            {
                switch (_sProperty.scrollDir)
                {
                    case ScrollDir.TopToBottom:
                        return _contentRect.anchoredPosition.y <= 1;
                    case ScrollDir.BottomToTop:
                        return _contentRect.anchoredPosition.y >= -1;
                    case ScrollDir.LeftToRight:
                        return _contentRect.anchoredPosition.x >= -1;
                    default:
                        return _contentRect.anchoredPosition.x <= 1;
                }
            }
        }
        /// <summary>
        /// 下区域判断
        /// </summary>
        protected bool isHighDefine
        {
            get
            {
                switch (_scrollRect.vertical)
                {
                    case true:
                        return Mathf.Abs(_contentRect.anchoredPosition.y) >=
                            (int)(_contentRect.rect.height - _viewRect.rect.height);
                    default:
                        return Mathf.Abs(_contentRect.anchoredPosition.x) >=
                            (int)(_contentRect.rect.width - _viewRect.rect.width);
                }
            }
        }
        #endregion
    }
}
