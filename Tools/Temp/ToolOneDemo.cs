﻿#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.InputNew;

namespace UnityEditor.Experimental.EditorVR.Tools
{
	[MainMenuItem("XTool1", "Create", "1 Extra Demo Tool")]
	//[MainMenuItem(false)]
	sealed class ToolOneDemo : MonoBehaviour, ITool, ICustomActionMap, IUsesRayOrigin, IUsesSpatialHash
	{
		public Transform rayOrigin { get; set; }

		public ActionMap actionMap
		{
			get { return m_ActionMap; }
			set { m_ActionMap = value; }
		}

		[SerializeField]
		private ActionMap m_ActionMap;

		public void ProcessInput(ActionMapInput input, ConsumeControlDelegate consumeControl)
		{
		}
	}
}
#endif