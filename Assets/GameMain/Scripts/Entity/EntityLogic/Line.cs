﻿using System;
using GameFramework.Event;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace GameMain
{
    public enum LineState
    {
        Undefined,
        NotConnect,
        Connect,
    }
    public class Line : Entity
    {
        [Header("Line")]
        [SerializeField] private LineState mLineState = LineState.Undefined;
        [SerializeField] private float mWidth = 1;
        private LineData m_Data = null;
        private LineRenderer m_LineRenderer = null;
        private Material m_Material = null;
        private bool m_LineValid = true;
        private PolygonCollider2D m_PolygonCollider2D = null;

        [Header("Mouse")]
        private float m_DepthZ = 10;
        private Vector3 m_MousePositionOnScreen = Vector3.zero;
        private Vector3 m_MousePositionInWorld = Vector3.zero; 
        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            GameEntry.Event.Subscribe(HideLineEventArgs.EventId,HideLine);
            GameEntry.Event.Subscribe(LineVaildEventArgs.EventId,LineValid);
            m_Data = userData as LineData;
            if (m_Data == null)
            {
                Log.Error("LineData object data is invalid.");
                return;
            }
            m_LineValid = true;
            m_PolygonCollider2D = transform.GetComponent<PolygonCollider2D>();
            mLineState = LineState.NotConnect;
            m_LineRenderer = transform.GetComponent<LineRenderer>();
            m_Material = m_LineRenderer.material;
            m_LineRenderer.SetPosition(0,m_Data.Self.position);
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            base.OnHide(isShutdown, userData);
            GameEntry.Event.Unsubscribe(HideLineEventArgs.EventId,HideLine);
            GameEntry.Event.Unsubscribe(LineVaildEventArgs.EventId,LineValid);
        }

        protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
        {
            base.OnUpdate(elapseSeconds, realElapseSeconds);
            switch (mLineState)
            {
                case LineState.Undefined:
                    break;
                case LineState.NotConnect:
                    GetMousePos(out m_MousePositionInWorld);
                    m_LineRenderer.SetPosition(1,m_MousePositionInWorld);
                    var transform1 = m_Data.Self.transform;
                    var self1Vector2 = new Vector2(transform1.position.x + mWidth, transform1.position.y);
                    var self2Vector2 = new Vector2(transform1.position.x - mWidth, transform1.position.y);
                    var target1Vector2 = new Vector2(m_MousePositionInWorld.x + mWidth, m_MousePositionInWorld.y);
                    var target2Vector2 = new Vector2(m_MousePositionInWorld.x - mWidth, m_MousePositionInWorld.y);
                    m_PolygonCollider2D.points =
                        new Vector2[] { self1Vector2, self2Vector2, target2Vector2, target1Vector2 };
                    
                    var distance = Vector3.Distance(m_LineRenderer.GetPosition(0),
                        m_LineRenderer.GetPosition(1));
                    //Debug.Log(distance);
                    var cost = (Math.Round(distance,1)) * UCS.BloodPerUnit;
                    if (m_LineValid && GameEntry.Utils.Blood >= cost)
                    {
                        m_Material.color = Color.white;
                        if (!Input.GetMouseButtonDown(0))
                            return;
                        RaycastHit2D hit = Physics2D.Raycast(m_MousePositionInWorld, Vector2.zero,
                            Mathf.Infinity,LayerMask.GetMask("Node"));
                        if (!hit)
                            return;
                        if (hit.collider.transform.position == m_Data.Self.position)
                            return;
                        var connectPair = new ConnectPair(m_Data.Self, hit.transform);
                        if (GameEntry.Utils.ConnectPairs.ContainsKey(connectPair))
                        {
                            if (GameEntry.Utils.ConnectPairs[connectPair])
                                return;
                        }
                        m_LineRenderer.SetPosition(1,hit.transform.position);
                        mLineState = LineState.Connect;
                        GameEntry.Utils.ConnectPairs.Add(new ConnectPair(m_Data.Self, hit.transform), true);
                        GameEntry.Utils.ConnectPairs.Add(new ConnectPair(hit.transform, m_Data.Self), true);
                        if (!GameEntry.Utils.LinePairs.ContainsKey(hit.transform))
                        {
                            GameEntry.Utils.LinePairs.Add(hit.transform,true);
                        }
                        
                        var selfNodeData = m_Data.Self.GetComponent<NodeData>();
                        var hitNodeData = hit.transform.GetComponent<NodeData>();
                        hitNodeData.NodeState = NodeState.Active;
                        if (!selfNodeData.ChildNodes.Contains(hitNodeData))
                        {
                            selfNodeData.ChildNodes.Add(hitNodeData);
                            GameEntry.Event.FireNow(this,AddChildNodeEventArgs.Create(selfNodeData));
                        }
                        if (!hitNodeData.ParentNodes.Contains(selfNodeData))
                        {
                            hitNodeData.ParentNodes.Add(selfNodeData);
                            GameEntry.Event.FireNow(this,AddParentNodeEventArgs.Create(hitNodeData));
                        }
                        GameEntry.Utils.Blood -= (int)cost;
                    }
                    else
                    {
                        m_Material.color = Color.red;
                    }
                    break;
                case LineState.Connect:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void GetMousePos(out Vector3 mousePositionInWorld)
        {
            m_MousePositionOnScreen = Input.mousePosition;
            m_MousePositionOnScreen.z = m_DepthZ;
            mousePositionInWorld = Camera.main.ScreenToWorldPoint(m_MousePositionOnScreen);
        }

        private void OnTriggerEnter(Collider other)
        {
            Entity entity = other.gameObject.GetComponent<Entity>();
            if (entity == null)
            {
                return;
            }
        }

        private void HideLine(object sender, GameEventArgs e)
        {
            HideLineEventArgs ne = (HideLineEventArgs)e;
            if (mLineState == LineState.Connect)
                return;
            GameEntry.Entity.HideEntity(Entity.Id);
        }

        private void LineValid(object sender, GameEventArgs e)
        {
            LineVaildEventArgs ne = (LineVaildEventArgs)e;
            if (mLineState == LineState.Connect)
                return;
            ClearNodeComponent clear = null;
            if (m_Data.Self.TryGetComponent(out clear))
                return;
            m_LineValid = ne.Valid;
        }
    }
}
