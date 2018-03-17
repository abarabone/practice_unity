using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;

public class AnimRecording : MonoBehaviour
{

	private IDisposable	ev; 

	private async void OnEnable()
	{
		var anim = GetComponent<Animator>();

		anim.StartRecording( 100 );

		await Observable.EveryGameObjectUpdate().Skip( 3 ).First();

		anim.StopRecording();
		anim.StartPlayback();
		anim.playbackTime = 0;

		ev = Observable.EveryGameObjectUpdate().Subscribe( _ => anim.playbackTime = 0 );
	}
	private void OnDisable()
	{
		GetComponent<Animator>().StopPlayback();
		ev.Dispose();
	}


}
