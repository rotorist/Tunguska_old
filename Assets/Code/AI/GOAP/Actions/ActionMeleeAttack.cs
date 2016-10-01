﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionMeleeAttack : GoapAction
{
	private float _attackTimer;
	private float _attackWaitTimeout;

	public ActionMeleeAttack(string name, string description, float cost)
	{
		Name = name;
		Description = description;
		Cost = cost;
		_preconditions = new List<GoapWorldState>();
		_effects = new List<GoapWorldState>();
	}

	public override bool ExecuteAction()
	{
		Debug.Log("Start executing melee attack " + ParentCharacter.name);

		_executionStopped = false;
		_attackTimer = 0;
		_attackWaitTimeout = UnityEngine.Random.Range(2f, 4f);

		UpdateAction();

		ParentCharacter.MyEventHandler.OnOneSecondTimer -= UpdateAction;
		ParentCharacter.MyEventHandler.OnOneSecondTimer += UpdateAction;
		ParentCharacter.MyEventHandler.OnPerFrameTimer -= PerFrameUpdate;
		ParentCharacter.MyEventHandler.OnPerFrameTimer += PerFrameUpdate;
		ParentCharacter.MyEventHandler.OnTakingHit -= OnTakingHit;
		ParentCharacter.MyEventHandler.OnTakingHit += OnTakingHit;

		ParentCharacter.MyAnimEventHandler.OnMeleeStrikeLeftFinish -= OnMeleeStrikeFinish;
		ParentCharacter.MyAnimEventHandler.OnMeleeStrikeLeftFinish += OnMeleeStrikeFinish;

		ParentCharacter.MyAnimEventHandler.OnMeleeStrikeRightFinish -= OnMeleeStrikeFinish;
		ParentCharacter.MyAnimEventHandler.OnMeleeStrikeRightFinish += OnMeleeStrikeFinish;

		ParentCharacter.MyAnimEventHandler.OnMeleeBlocked -= OnMeleeStrikeFinish;
		ParentCharacter.MyAnimEventHandler.OnMeleeBlocked += OnMeleeStrikeFinish;



		return true;
	}

	public override void StopAction()
	{
		Debug.Log("Stop executing melee attack " + ParentCharacter.name);
		_executionStopped = true;
		if(ParentCharacter.MyAnimator.GetInteger("AlertLevel") >= 2)
		{
			ParentCharacter.MyAnimator.SetInteger("AlertLevel", 1);
		}

		ParentCharacter.MyEventHandler.OnOneSecondTimer -= UpdateAction;
		ParentCharacter.MyEventHandler.OnPerFrameTimer -= PerFrameUpdate;
		ParentCharacter.MyEventHandler.OnTakingHit -= OnTakingHit;
		ParentCharacter.MyAnimEventHandler.OnMeleeStrikeLeftFinish -= OnMeleeStrikeFinish;
		ParentCharacter.MyAnimEventHandler.OnMeleeStrikeRightFinish -= OnMeleeStrikeFinish;
		ParentCharacter.MyAnimEventHandler.OnMeleeBlocked -= OnMeleeStrikeFinish;
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
		if(ParentCharacter.MyAI.BlackBoard.GuardLevel == 0)
		{
			return true;
		}

		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy == null)
		{
			return true;
		}

		foreach(GoapWorldState state in Effects)
		{

			object result = ParentCharacter.MyAI.EvaluateWorldState(state);
			//Debug.Log("Checking if state " + state.Name + " value is " + state.Value + " result: " + result);
			if(!result.Equals(state.Value))
			{
				//Debug.Log("result is not equal to effect");
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


		//CsDebug.Inst.CharLog(ParentCharacter, "Checking melee attack precondition, pass");
		return true;

	}


	public void UpdateAction()
	{
		if(!CheckAvailability() || _executionStopped)
		{
			return;
		}
			
		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy != null)
		{
			float dist = Vector3.Distance(ParentCharacter.transform.position, ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position);
			if(dist > 5f)
			{
				
				ParentCharacter.SendCommand(CharacterCommands.GoToPosition);
				ParentCharacter.CurrentStance = HumanStances.Run;
			}
			else if(dist > 1.6f)
			{
				ParentCharacter.SendCommand(CharacterCommands.GoToPosition);
				ParentCharacter.CurrentStance = HumanStances.Walk;
			}
		}
		if(_attackTimer >= 0 && _attackTimer < _attackWaitTimeout)
		{
			_attackTimer += 1;
		}

		if(CheckActionCompletion())
		{
			StopAction();

			ParentCharacter.MyEventHandler.TriggerOnActionCompletion();
		}

	}

	public void PerFrameUpdate()
	{
		if(ParentCharacter.MyAI.BlackBoard.TargetEnemy != null)
		{
			//check if is in range
			float dist = Vector3.Distance(ParentCharacter.transform.position, ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position);
			Vector3 targetVelocity = ParentCharacter.MyAI.BlackBoard.TargetEnemy.MyNavAgent.velocity;

			if(targetVelocity.magnitude >= 0.5f)
			{
				if(dist > 1.2f)
				{
					//go to target enemy
					ParentCharacter.MyAI.BlackBoard.NavTarget = ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position + targetVelocity * 0.1f;
					ParentCharacter.Destination = ParentCharacter.MyAI.BlackBoard.NavTarget;
					ParentCharacter.CurrentStance = HumanStances.Run;
				}
				else
				{
					if(_attackTimer >= _attackWaitTimeout)
					{
						ParentCharacter.SendCommand(CharacterCommands.RunningAttack);
						_attackTimer = -1;
						_attackWaitTimeout = UnityEngine.Random.Range(1f, 3f);
					}
				}

				if(ParentCharacter.MyAnimator.GetInteger("AlertLevel") >= 2)
				{
					ParentCharacter.MyAnimator.SetInteger("AlertLevel", 1);
				}
			}
			else
			{
				if(dist > 5f)
				{
					ParentCharacter.MyAI.BlackBoard.NavTarget = ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position;
					ParentCharacter.Destination = ParentCharacter.MyAI.BlackBoard.NavTarget;
					ParentCharacter.CurrentStance = HumanStances.Run;

					if(ParentCharacter.MyAnimator.GetInteger("AlertLevel") >= 2)
					{
						ParentCharacter.MyAnimator.SetInteger("AlertLevel", 1);
					}
				}
				else if(dist > 1.6f)
				{
					ParentCharacter.MyAI.BlackBoard.NavTarget = ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position;
					ParentCharacter.Destination = ParentCharacter.MyAI.BlackBoard.NavTarget;
					ParentCharacter.CurrentStance = HumanStances.Walk;

					if(ParentCharacter.MyAnimator.GetInteger("AlertLevel") >= 2)
					{
						ParentCharacter.MyAnimator.SetInteger("AlertLevel", 1);
					}
				}
				else
				{
					if(ParentCharacter.MyAnimator.GetInteger("AlertLevel") < 2)
					{
						ParentCharacter.MyAnimator.SetInteger("AlertLevel", 2);
					}

					ParentCharacter.SendCommand(CharacterCommands.Idle);
					if(_attackTimer >= _attackWaitTimeout)
					{
						
						float attackType = UnityEngine.Random.value;
						if(attackType < 0.3f)
						{
							ParentCharacter.SendCommand(CharacterCommands.LeftAttack);
						}
						else if(attackType < 0.7f)
						{
							ParentCharacter.SendCommand(CharacterCommands.RightAttack);
						}
						else
						{
							ParentCharacter.SendCommand(CharacterCommands.QuickAttack);
						}
						_attackTimer = -1;
						_attackWaitTimeout = UnityEngine.Random.Range(1f, 3f);
					}
				}
			}
				


		}
	}


	public void OnMeleeStrikeFinish()
	{
		_attackTimer = 0;

	}

	public void OnTakingHit()
	{
		_attackTimer = 0;
		_attackWaitTimeout = UnityEngine.Random.Range(0.2f, 1f);
	}

	private bool CheckAvailability()
	{
		if(ParentCharacter.IsBodyLocked)
		{
			return false;
		}
		else
		{
			return true;
		}
	}

}
