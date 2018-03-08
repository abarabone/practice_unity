using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

using PracticeUnity;

public class Sample : MonoBehaviour
{
	
	public ICharacterActionModel	ch;


	// Use this for initialization
	void Start ()
	{
		Observable.EveryGameObjectUpdate().Subscribe( x => transform.Translate( 0,0.01f,0 ) );
	}
	
}
