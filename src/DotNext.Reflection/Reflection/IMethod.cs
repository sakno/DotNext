﻿using System;
using System.Reflection;

namespace DotNext.Reflection
{
	
	public interface IMethod<out M, out D>: IMember<M, D>
		where M: MethodBase
		where D: Delegate
	{
	}

	/// <summary>
	/// Represents regular method.
	/// </summary>
	/// <typeparam name="D">Type of delegate describing method signature.</typeparam>
	public interface IMethod<out D>: IMethod<MethodInfo, D>
		where D: Delegate
	{

	}
}