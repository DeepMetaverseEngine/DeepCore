using DeepCore.EventTrigger;
using DeepCore.EventTrigger.Data;
using DeepCore.Game3D.Host.Instance;
using DeepCore.GameData.EventTrigger;
using DeepCore.Reflection;
using System;
using System.Collections.Generic;
using System.Text;
using static DeepCore.Colors;

namespace DeepCore.GameData.Zone.ZoneEditor.EventTrigger
{

	public abstract class UnitSetTimeMS : ZoneAbstractAction
	{
		[Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
		[Desc("时间（毫秒）")] public AbstractValue<double> TimeMS = new RealValue.VALUE(3000);
		protected override void GetText(EventStringBuilder sw)
		{
			sw.AppendFormat("单位({0}){1}{2}毫秒;", Unit, GetType().GetAttribute<DescAttribute>().Desc, TimeMS);
		}
		override protected object Run(IEventTriggerAdapter api, EventArguments args)
		{
			var unit = Unit.GetValueAs(api, args);
			if (unit != null)
			{
				SetTime(unit, (float)TimeMS.GetValueAs(api, args));
			}
			return unit;
		}
		public abstract void SetTime(InstanceUnit unit, float timeMS);
	}

	public abstract class UnitCleanTimeMS : ZoneAbstractAction
	{
		[Desc("单位")] public AbstractValue<InstanceUnit> Unit = new UnitValue.Trigging();
		protected override void GetText(EventStringBuilder sw)
		{
			sw.AppendFormat("单位({0}){1};", Unit, GetType().GetAttribute<DescAttribute>().Desc);
		}
		override protected object Run(IEventTriggerAdapter api, EventArguments args)
		{
			var unit = Unit.GetValueAs(api, args);
			if (unit != null)
			{
				ClearTime(unit);
			}
			return unit;
		}
		public abstract void ClearTime(InstanceUnit unit);
	}

	//-----------------------------------------------------------------------------------------------------------------
	[Desc("设置霸体时间", "[游戏]/单位/时效性效果")]
	public class UnitSetNoneBlockTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetNoneBlockTimeMS(timeMS);
	}
	[Desc("设置眩晕时间", "[游戏]/单位/时效性效果")]
	public class UnitSetStunTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetStunTimeMS(timeMS);
	}
	[Desc("设置隐身时间", "[游戏]/单位/时效性效果")]
	public class UnitSetInvisibleTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetInvisibleTimeMS(timeMS);	
	}
	[Desc("设置无敌时间", "[游戏]/单位/时效性效果")]
	public class UnitSetInvincibleTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetInvincibleTimeMS(timeMS);
	}
	[Desc("设置无伤时间", "[游戏]/单位/时效性效果")]
	public class UnitSetNoDamageTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetNoDamageTimeMS(timeMS);
	}
	[Desc("设置沉默时间", "[游戏]/单位/时效性效果")]
	public class UnitSetSilentTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetSilentTimeMS(timeMS);
	}
	[Desc("设置锁住位移时间", "[游戏]/单位/时效性效果")]
	public class UnitSetLockTimeMS : UnitSetTimeMS
	{
		public override void SetTime(InstanceUnit unit, float timeMS) => unit.SetLockTimeMS(timeMS);
	}
	//-----------------------------------------------------------------------------------------------------------------
	[Desc("清除霸体时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanNoneBlockTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearNoneBlock();
	}
	[Desc("清除眩晕时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanStunTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearStun();
	}
	[Desc("清除隐身时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanInvisibleTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearInvisible();
	}
	[Desc("清除无敌时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanInvincibleTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearInvincible();
	}
	[Desc("清除无伤时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanNoDamageTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearNoDamage();
	}
	[Desc("清除沉默时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanSilentTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearSilent();
	}
	[Desc("清除锁住位移时间", "[游戏]/单位/时效性效果")]
	public class UnitCleanLockTimeMS : UnitCleanTimeMS
	{
		public override void ClearTime(InstanceUnit unit) => unit.ClearLock();
	}
	//-----------------------------------------------------------------------------------------------------------------


}
