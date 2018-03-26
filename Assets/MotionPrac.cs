using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
using UniRx;

public class MotionPrac : MonoBehaviour
{

	private PlayableGraph	graph;

	public AnimationClip	Clip;
	

	private void Start()
	{
		this.Clip.legacy = true;
		this.Clip.legacy = false;
		
		var p = AnimationClipPlayable.Create( this.graph, this.Clip );

		var o = AnimationPlayableOutput.Create( this.graph, "a", this.GetComponent<Animator>() );

		o.SetSourcePlayable( p );

		graph.Play();

		Observable.Interval( System.TimeSpan.FromSeconds( 2.0f ) )
			.Subscribe( _ => p.SetTime( 0.0f ) );
	}

	AnimationClip createNewClip( AnimationClip cp )
	{
		var newcp = new AnimationClip();
		newcp.
	}


	private void Awake()
	{
		graph = PlayableGraph.Create();
	}
	private void OnDestroy()
	{
		graph.Destroy();
	}
}
