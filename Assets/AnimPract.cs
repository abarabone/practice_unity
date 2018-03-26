using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UniRx;
using UniRx.Triggers;
using UniRx.Operators;
using UniRx.Toolkit;

public class AnimPract : MonoBehaviour
{

	PlayableGraph			graph;
	AnimationMixerPlayable	mixer;
	
	public AnimationClip	Clip1;
	public AnimationClip	Clip2;
	public AnimationClip    Clip3;
	
	public NewPlayableBehaviour pb = new NewPlayableBehaviour();
	public ScriptPlayable<NewPlayableBehaviour>	sp;

	[RangeReactiveProperty(0,1)]
	public FloatReactiveProperty	weight;


	private void Awake()
	{
		graph = PlayableGraph.Create();

		//graph.SetTimeUpdateMode( DirectorUpdateMode.GameTime );

		sp = ScriptPlayable<NewPlayableBehaviour>.Create( graph, pb );

	}
	private void Start()
	{

		// AnimationClipPlayableを構築

		var clip1Playable = AnimationClipPlayable.Create (graph, Clip1);
		var clip2Playable = AnimationClipPlayable.Create (graph, Clip2);
		var clip3Playable = AnimationClipPlayable.Create (graph, Clip3);

		

		// ミキサーを生成して、Clip1とClip2を登録
		// （代わりにAnimatorControllerPlayableとかでも可能）

		mixer = AnimationMixerPlayable.Create( graph, 2, true );
		mixer.ConnectInput( 0, clip1Playable, 0 );
		mixer.ConnectInput( 1, clip2Playable, 0 );


		// outputを生成して、出力先を自身のAnimatorに設定

		var output = AnimationPlayableOutput.Create (graph, "output", GetComponent<Animator>());


		// playableをoutputに流し込む

		output.SetSourcePlayable( mixer );
		

		graph.Play();

	}
	private void Update()
	{
		mixer.SetInputWeight( 0, weight.Value );
		mixer.SetInputWeight( 1, 1.0f - weight.Value );
	}

	private void OnEnable()
	{
		graph.Play();
	}
	private void OnDisable()
	{
		graph.Stop();
	}

	private void OnDestroy()
	{
		graph.Destroy();
	}
}

