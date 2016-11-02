﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionMutantBite : GoapAction
{
	private bool _isBiting;

	public ActionMutantBite(string name, string description, float cost)
	{
		Name = name;
		Description = description;
		Cost = cost;
		_preconditions = new List<GoapWorldState>();
		_effects = new List<GoapWorldState>();
	}

	public override bool ExecuteAction()
	{
		Debug.Log("Start executing ActionMutantBite " + ParentCharacter.name);

		_executionStopped = false;
		_isBiting = false;




		UpdateAction();

		ParentCharacter.MyEventHandler.OnOneSecondTimer -= UpdateAction;
		ParentCharacter.MyEventHandler.OnOneSecondTimer += UpdateAction;
		ParentCharacter.MyEventHandler.OnPerFrameTimer -= PerFrameUpdate;
		ParentCharacter.MyEventHandler.OnPerFrameTimer += PerFrameUpdate;






		return true;
	}

	public override void StopAction()
	{
		Debug.Log("Stop executing ActionMutantBite " + ParentCharacter.name);
		_executionStopped = true;

		ParentCharacter.MyEventHandler.OnOneSecondTimer -= UpdateAction;
		ParentCharacter.MyEventHandler.OnPerFrameTimer -= PerFrameUpdate;

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

	public override float GetActionCost ()
	{
		return UnityEngine.Random.Range(1f, 1f);
	}

	public override bool CheckContextPrecondition ()
	{

		Debug.Log("ranged attack precondition; enemy threat is " + ParentCharacter.MyAI.BlackBoard.TargetEnemyThreat);
		if(ParentCharacter.MyAI.BlackBoard.GuardLevel == 0)
		{
			return false;
		}

		Character target = ParentCharacter.MyAI.BlackBoard.TargetEnemy;

		if(target == null)
		{
			return false;
		}

		//check if target's back is facing me

		Vector3 myDir = new Vector3(ParentCharacter.transform.forward.x, 0, ParentCharacter.transform.forward.z);
		Vector3 targetDir = new Vector3(target.transform.forward.x, 0, target.transform.forward.z);
		if(Vector3.Angle(myDir, targetDir) > 60)
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
			if(dist > 1f && !_isBiting)
			{
				//go to target enemy
				ParentCharacter.MyAI.BlackBoard.NavTarget = ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.position - ParentCharacter.MyAI.BlackBoard.TargetEnemy.transform.forward * 0.5f;
				ParentCharacter.Destination = ParentCharacter.MyAI.BlackBoard.NavTarget;
				ParentCharacter.CurrentStance = HumanStances.Run;
				ParentCharacter.SendCommand(CharacterCommands.GoToPosition);
			}
			else
			{
				Debug.Log("Ready to bite");
				ParentCharacter.SendCommand(CharacterCommands.Bite);
			}

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





		}
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
