﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionRangedAttack : GoapAction
{
	private float _exitDelayTimer;

	enum ManeuverState
	{
		Shuffle,
		MoveAway,
		MoveTowards,
	}

	private ManeuverState _maneuverState;

	public ActionRangedAttack(string name, string description, float cost)
	{
		Name = name;
		Description = description;
		Cost = cost;
		_preconditions = new List<GoapWorldState>();
		_effects = new List<GoapWorldState>();
	}

	public override bool ExecuteAction()
	{
		CsDebug.Inst.CharLog(ParentCharacter, "Start executing Ranged Attack action " + ParentCharacter.name);

		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy == null)
		{
			return false;
		}

		_executionStopped = false;
		_readyForCompletion = false;
		_exitDelayTimer = 0;

		ParentCharacter.SendCommand(CharacterCommands.Aim);
		((HumanCharacter)ParentCharacter).CurrentStance = HumanStances.Walk;
		_maneuverState = ManeuverState.MoveTowards;

		ParentCharacter.MyEventHandler.OnActionUpdateTimer -= UpdateAction;
		ParentCharacter.MyEventHandler.OnActionUpdateTimer += UpdateAction;

		UpdateAction();

		return true;
	}

	public override void StopAction()
	{
		CsDebug.Inst.CharLog(ParentCharacter, "Stop executing Ranged Attack " + ParentCharacter.name);
		_executionStopped = true;
		ParentCharacter.MyAI.WeaponSystem.StopFiringRangedWeapon();
		ParentCharacter.MyEventHandler.OnActionUpdateTimer -= UpdateAction;
	}

	public override bool AbortAction (float priority)
	{
		if(priority >= 1)
		{
			StopAction();
			return true;
		}
		else
		{
			return false;
		}
	}

	public override bool CheckActionCompletion()
	{
		if(!_readyForCompletion)
		{
			return false;
		}

		if(ParentCharacter.MyAI.BlackBoard.GuardLevel == 0)
		{
			return true;
		}

		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy == null)
		{
			return true;
		}

		//if top priority goal is follow, check if i'm too far from follow target or guard target
		GoapGoal top = ParentCharacter.MyAI.GetDynamicGoal(0);
		if(top != null && top.Name == "Follow target" && ParentCharacter.MyAI.BlackBoard.GuardLevel <= 0)
		{
			if(Vector3.Distance(ParentCharacter.transform.position, ParentCharacter.MyAI.BlackBoard.FollowTarget.transform.position) > 6)
			{
				return true;
			}
		}


		foreach(GoapWorldState state in Effects)
		{

			object result = ParentCharacter.MyAI.EvaluateWorldState(state);
			//CsDebug.Inst.CharLog(ParentCharacter, "Checking if state " + state.Name + " value is " + state.Value + " result: " + result);
			if(!result.Equals(state.Value))
			{
				//CsDebug.Inst.CharLog(ParentCharacter, "result is not equal to effect");
				return false;
			}
		}



		return true;
	}

	public override bool CheckContextPrecondition ()
	{
		//Debug.Log("ranged attack precondition; enemy threat is " + ParentCharacter.MyAI.BlackBoard.TargetEnemyThreat);
		if(ParentCharacter.MyAI.BlackBoard.GuardLevel == 0)
		{
			return false;
		}

		if(ParentCharacter.MyAI.BlackBoard.TargetEnemyThreat < 0.66f)
		{
			return false;
		}


		//CsDebug.Inst.CharLog(ParentCharacter, "Checking ranged attack precondition, pass");
		return true;
	}

	public override float GetActionCost ()
	{
		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy != null)
		{
			if(Vector3.Angle(ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.forward, ParentCharacter.transform.forward) > 45)
			{
				return Cost/2;
			}
			else
			{
				return Cost;
			}
		}
		else
		{
			return Cost;
		}
	}

	public void UpdateAction()
	{
		//continue to check if body is locked, i.e. wait till it's not locked
		if(!CheckAvailability() || _executionStopped)
		{
			return;
		}





		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy != null)
		{
			//check if is in range
			float dist = Vector3.Distance(ParentCharacter.transform.position, ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position);
			float gunRange = ParentCharacter.MyReference.CurrentWeapon.GetComponent<GunBarrel>().Range;
			Weapon currentWeapon = ParentCharacter.MyAI.WeaponSystem.GetCurrentWeapon();

			float threat = ParentCharacter.MyAI.BlackBoard.HighestPersonalThreat;
			Vector3 threatDir = ParentCharacter.MyAI.BlackBoard.AvgPersonalThreatDir;

			if(_maneuverState == ManeuverState.MoveTowards)
			{
				//Debug.Log("maneuver state " + _maneuverState);
				if(dist < gunRange)
				{
					_maneuverState = ManeuverState.Shuffle;
				}
				else
				{
					//if using secondary weapon, switch to primary
					if(currentWeapon != null && currentWeapon.WeaponItem.Type == ItemType.SideArm && ParentCharacter.MyAI.WeaponSystem.PrimaryWeapon != null)
					{
						ParentCharacter.MyAI.WeaponSystem.StopFiringRangedWeapon();
						ParentCharacter.SendCommand(CharacterCommands.SwitchWeapon2);
					}
					else
					{
						//run towards enemy
						ParentCharacter.MyAI.WeaponSystem.StopFiringRangedWeapon();
						ParentCharacter.SendCommand(CharacterCommands.StopAim);
						ParentCharacter.MyAI.BlackBoard.NavTarget = ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position;
						ParentCharacter.CurrentStance = HumanStances.Run;
					}
				}
			}
			else if(_maneuverState == ManeuverState.MoveAway)
			{
				//Debug.Log("maneuver state " + _maneuverState);
				if(dist > gunRange * 0.75f)
				{
					_maneuverState = ManeuverState.Shuffle;
				}
				else if(dist > gunRange)
				{
					_maneuverState = ManeuverState.MoveTowards;
				}
				else
				{
					//shuffle backwards while aiming 
					Vector3 backwardDist = (ParentCharacter.transform.position - ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position).normalized;
					int rand = UnityEngine.Random.Range(0, 100);
					int leftRight = (rand >= 50) ? -1 : 1;
					Vector3 shuffleCenter = ParentCharacter.transform.position + backwardDist * 3 + Vector3.Cross(threatDir, Vector3.up) * 3 * leftRight;
					Vector3 shuffleDest = Vector3.zero;
					AI.RandomPoint(shuffleCenter, new Vector3(2, 2, 2), out shuffleDest);
					ParentCharacter.MyAI.BlackBoard.NavTarget = shuffleDest;
					ParentCharacter.SendCommand(CharacterCommands.Aim);
					ParentCharacter.CurrentStance = HumanStances.Walk;
				}
			}
			else
			{
				//Debug.Log("maneuver state " + _maneuverState);
				if(dist > gunRange)
				{
					_maneuverState = ManeuverState.MoveTowards;
				}
				else if(dist < gunRange * 0.25f)
				{
					bool isScoped = ParentCharacter.MyAI.WeaponSystem.IsCurrentWeaponScoped();

					//if scoped then pull out secondary weapon when close
					if(isScoped && ParentCharacter.MyAI.WeaponSystem.SideArm != null)
					{
						ParentCharacter.MyAI.WeaponSystem.StopFiringRangedWeapon();
						ParentCharacter.SendCommand(CharacterCommands.SwitchWeapon1);
					}
					else 
					{
						_maneuverState = ManeuverState.MoveAway;
					}
				}
				else
				{
					Vector3 shuffleCenter = ParentCharacter.transform.position;
					Vector3 shuffleDest = shuffleCenter;

					if(threat >= 0 && UnityEngine.Random.value > 0.4f)
					{
						//Debug.Log("shuffling");
						int rand = UnityEngine.Random.Range(0, 100);
						int leftRight = (rand >= 50) ? -1 : 1;
						shuffleCenter = ParentCharacter.transform.position + Vector3.Cross(threatDir, Vector3.up) * 3 * leftRight;
						shuffleDest = Vector3.zero;
						AI.RandomPoint(shuffleCenter, new Vector3(2, 2, 2), out shuffleDest);

					}

					ParentCharacter.MyAI.BlackBoard.NavTarget = shuffleDest;
					ParentCharacter.SendCommand(CharacterCommands.Aim);
					ParentCharacter.CurrentStance = HumanStances.Walk;
				}
			}

			if(((HumanCharacter)ParentCharacter).UpperBodyState == HumanUpperBodyStates.Aim && dist < gunRange)
			{
				if(ParentCharacter.MyAI.WeaponSystem.AIWeaponState != AIWeaponStates.FiringRangedWeapon)
				{
					ParentCharacter.MyAI.WeaponSystem.StartFiringRangedWeapon();
				}
			}

			ParentCharacter.Destination = ParentCharacter.MyAI.BlackBoard.NavTarget;
			ParentCharacter.SendCommand(CharacterCommands.GoToPosition);

		}
		else
		{
			ParentCharacter.MyAI.WeaponSystem.StopFiringRangedWeapon();
		}


		if(CheckActionCompletion() && _exitDelayTimer > 5)
		{
			StopAction();

			ParentCharacter.MyEventHandler.TriggerOnActionCompletion();
		}

		_readyForCompletion = true;
		_exitDelayTimer += 1f;
	}

	private bool CheckAvailability()
	{
		//check if body is locked
		if(ParentCharacter.IsBodyLocked || ParentCharacter.MyReference.CurrentWeapon == null)
		{
			return false;
		}
		else
		{
			return true;
		}

	}
}
