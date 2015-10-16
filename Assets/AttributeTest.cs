using UnityEngine;
using System;
using System.Reflection;

[AttributeUsage(AttributeTargets.Method)]
public class MyAttribute : System.Attribute {
	public string s;
	
	public MyAttribute (string s) {
		this.s = s;
	}
}

public class AttributeTest : MonoBehaviour {
	BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
	
	void Start () {
		foreach (MethodInfo m in typeof(AttributeTest).GetMethods(flags)) {
			foreach (Attribute a in m.GetCustomAttributes(true)) {
				MyAttribute ma = a as MyAttribute;
				if (ma != null) {
					Debug.Log(ma.s);
				}			
			}
		}
	}
	
	[My("Hello World")]
	private void SomeMethod () {
	
	}
	
	[MyAttribute("This is another message!")]
	private void SomeMethod(int x) {
	}
}
