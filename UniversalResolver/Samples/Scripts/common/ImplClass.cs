﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityIoC;

namespace SceneTest
{
	public class ImplClass : AbstractClass
	{

		public void DoSomething()
		{
			MyDebug.Log("This is ImplClass");
		}
	}
}