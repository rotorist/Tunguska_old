﻿using UnityEngine;
using System.Collections;

public class WeaponSwitchNotify : StateMachineBehaviour {

	 // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
	//override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
	//override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateExit is called when a transition ends and the state machine finishes evaluating this state
	override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) 
	{
		if(stateInfo.IsName("LongGunPullOut"))
		{
			animator.GetComponent<CharacterReference>().ParentCharacter.MyAnimEventHandler.TriggerOnLongGunPullOutFinish();
		}
		else if(stateInfo.IsName("SmallWeaponPullOut"))
		{
			animator.GetComponent<CharacterReference>().ParentCharacter.MyAnimEventHandler.TriggerOnPistolPullOutFinish();
		}
		else if(stateInfo.IsName("LongGunPutAway"))
		{
			animator.GetComponent<CharacterReference>().ParentCharacter.MyAnimEventHandler.TriggerOnLongGunPutAwayFinish();
		}
		else if(stateInfo.IsName("SmallWeaponPutAway"))
		{
			animator.GetComponent<CharacterReference>().ParentCharacter.MyAnimEventHandler.TriggerOnPistolPutAwayFinish();
		}
		else if(stateInfo.IsName("GrenadePullOut"))
		{
			animator.GetComponent<CharacterReference>().ParentCharacter.MyAnimEventHandler.TriggerOnGrenadePullOutFinish();
		}
		else if(stateInfo.IsName("MeleeDraw"))
		{
			animator.GetComponent<CharacterReference>().ParentCharacter.MyAnimEventHandler.TriggerOnMeleePullOutFinish();
		}
	}

	// OnStateMove is called right after Animator.OnAnimatorMove(). Code that processes and affects root motion should be implemented here
	//override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}

	// OnStateIK is called right after Animator.OnAnimatorIK(). Code that sets up animation IK (inverse kinematics) should be implemented here.
	//override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex) {
	//
	//}
}
