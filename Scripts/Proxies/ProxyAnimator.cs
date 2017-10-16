﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.EditorVR;
using UnityEditor.Experimental.EditorVR.Core;
using UnityEditor.Experimental.EditorVR.Proxies;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;
using UnityEngine.InputNew;

[ProcessInput(1)]
[RequireComponent(typeof(ProxyHelper))]
public class ProxyAnimator : MonoBehaviour, ICustomActionMap
{
    public class TransformInfo
    {
        public Vector3 initialPosition;
        public Vector3 initialRotation;
        public Vector3 positionOffset;
        public Vector3 rotationOffset;

        public void Apply(Transform transform)
        {
            transform.localPosition = initialPosition + positionOffset;
            transform.localRotation = Quaternion.Euler(initialRotation + rotationOffset);
        }
    }

    [SerializeField]
    ActionMap m_ProxyActionMap;

    [SerializeField]
    ProxyAffordanceMap m_AffordanceMap;

    Affordance[] m_Affordances;
    ProxyAffordanceMap.AffordanceDefinition[] m_AffordanceDefinitions;
    InputControl[] m_Controls;

    readonly Dictionary<Transform, TransformInfo> m_TransformInfos = new Dictionary<Transform, TransformInfo>();

    public ActionMap actionMap { get { return m_ProxyActionMap; } }
    public bool ignoreLocking { get { return true; } }
    internal event Action<Affordance[], ProxyAffordanceMap.AffordanceDefinition[], Dictionary<Transform, TransformInfo>, ActionMapInput> postAnimate;

    void Start()
    {
        m_Affordances = GetComponent<ProxyHelper>().affordances;
        m_AffordanceDefinitions = m_AffordanceMap.AffordanceDefinitions;
    }

    public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
    {
        if (m_Affordances == null)
            return;

        var length = m_Affordances.Length;
        if (m_Controls == null)
        {
            m_Controls = new InputControl[length];

            var bindings = input.actionMap.controlSchemes[0].bindings;
            for (var i = 0; i < input.controlCount; i++)
            {
                var control = input[i];
                var binding = bindings[i];
                for (var j = 0; j < length; j++)
                {
                    var affordance = m_Affordances[j];
                    foreach (var index in binding.sources)
                    {
                        if (index.controlIndex == (int)affordance.control)
                        {
                            m_Controls[j] = control;
                            break;
                        }
                    }
                }
            }

            foreach (var affordance in m_Affordances)
            {
                var affordanceTransform = affordance.transform;
                TransformInfo info;
                if (!m_TransformInfos.TryGetValue(affordanceTransform, out info))
                {
                    info = new TransformInfo();
                    m_TransformInfos[affordanceTransform] = info;
                }

                info.initialPosition = affordanceTransform.localPosition;
                info.initialRotation = affordanceTransform.localRotation.eulerAngles;
            }
        }

        foreach (var kvp in m_TransformInfos)
        {
            var transformInfo = kvp.Value;
            transformInfo.positionOffset = Vector3.zero;
            transformInfo.rotationOffset = Vector3.zero;
        }

        for (var i = 0; i < length; i++)
        {
            var affordance = m_Affordances[i];
            var control = m_Controls[i];
            var affordanceDefinition = m_AffordanceDefinitions.Where(x => x.control == affordance.control).FirstOrDefault();
            var animationDefinition = affordanceDefinition != null ? affordanceDefinition.animationDefinition : null;
            var info = m_TransformInfos[affordance.transform];

            // Animate any values defined in the ProxyAffordanceMap's Affordance Definition
            //Assume control values are [-1, 1]
            if (animationDefinition != null)
            {
                var min = animationDefinition.min;
                var offset = min + (control.rawValue + 1) * (animationDefinition.max - min) * 0.5f;

                info.positionOffset += animationDefinition.translateAxes.GetAxis() * offset;
                info.rotationOffset += animationDefinition.rotateAxes.GetAxis() * offset;
            }
        }

        foreach (var kvp in m_TransformInfos)
        {
            kvp.Value.Apply(kvp.Key);
        }

        if (postAnimate != null)
            postAnimate(m_Affordances, m_AffordanceDefinitions, m_TransformInfos, input);
    }
}