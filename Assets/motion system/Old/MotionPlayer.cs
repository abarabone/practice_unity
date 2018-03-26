using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Text;







[System.Serializable]
//public class MotionPlayer : MonoBehaviour
public struct MotionPlayer
{

	public MotionClips	clips;


	Motions		motions;

	bool isLoop;

	float	limitTime;


	float	pastTime;

	public float	motionSpeed;


	public StreamKeyHolder	streamStates;


	public PostureUnit[]	postures;	// 最後部（ボーン数 + 1）にはルート位置情報が入る。
	public Matrix4x4[]	mtPostures;

	[SerializeField]
	//[HideInInspector]
	BoneLinker	boneLinks;

	public Transform[]	notRemveTransforms;


	_Action3	act;

	SkinnedMeshRender	drawer;



	public float time { get { return pastTime; } }

	public bool isFinished { get { return pastTime > limitTime; } }



	public
	enum EnStreamMode
	{
		pos1rotFull	= 0,
		fullStream	= 1
	}

	public enum EnKeyMode
	{
		rotation	= 0,
		position	= 1,
		rot = rotation,
		pos = position
	}





	// トリムする時はそれによって生成されたワークはシリアライズ可であること。
	// それによって、複製しても運用できるようにする。
	// トリムしない場合はＴＦに結果を書きだし、した場合はワークに書き出す。
	// また、ローカルスケールが変化しているＴＦから、スケール値を保存することが必要になる。
	// 一部のＴＦはトリムせずに書き込みが必要。
	// 　・Rigidbody がある
	// 　・コライダーがある
	// 　・なんらかのコンポーネント・スクリプトがついている
	// 　・除外リストに載っている
	// 





	public void build( _Action3 action, Type type, Type EnTypeMotion )
	{
		build( action, type, EnTypeMotion, MotionPlayer.EnStreamMode.fullStream );
	}

	public void buildLite( _Action3 action, Type type, Type EnTypeMotion )
	{
		build( action, type, EnTypeMotion, MotionPlayer.EnStreamMode.pos1rotFull );
	}

	void build( _Action3 action, Type type, Type EnTypeMotion, EnStreamMode enMode )
	{

		act = action;


		var tfBones = getTfBonesFromRenderSystem();

		motions = clips.getMotions( type, EnTypeMotion, tfBones );


		streamStates = new StreamKeyHolder( motions.streamLength, enMode );
		
		postures = new PostureUnit[ tfBones.Length + 1 ];
		//mtPostures = new Matrix4x4[ tfBones.Length + 1 ];


		if( !streamStates.isReady )
		{

			boneLinks.scanLinks( tfBones, notRemveTransforms );

		}
	}


	Transform[] getTfBonesFromRenderSystem()
	{

		var mysmr = act.tfBody.GetComponentInChildren<SkinnedMeshRender>();

		if( mysmr )
		{
			mysmr.enabled = false;

			drawer = mysmr;

			return mysmr.bones;
		}

		var smr = act.tfBody.GetComponentInChildren<SkinnedMeshRenderer>();

		if( smr )
		{
			
			return smr.bones;
		}

		return null;
	}




	public void init( int motionId )
	{
		pastTime = 0.0f;


		play( motionId, true );

	}


	public void play( int motionId, bool _isLoop = false )
	{
		pastTime = 0.0f;

		limitTime = motions[ motionId ].time;

		isLoop = _isLoop;

		for( var i = 0 ; i < streamStates.length ; i++ )
		{
			var motion = motions[ motionId ];

			streamStates.streams[ i ].set( motions, motionId, isLoop );
		}
	}

	public void shiftTo( int motionId, bool _isLoop = false )
	{
		pastTime = 0.0f;

		limitTime = motions[ motionId ].time;

		isLoop = _isLoop;

		for( var i = 0 ; i < streamStates.length ; i++ )
		{
			var motion = motions[ motionId ];

			streamStates.streams[ i ].shiftTo( motions, motionId, isLoop );
		}
	}



	public bool animate()// float speed = 1.0f )
	{

		pastTime += GM.t.delta * motionSpeed;


		for( var i = 0 ; i < streamStates.length ; i++ )
		{
			streamStates.streams[ i ].progress( pastTime, motions, isLoop );
		}


		var isLimitOver = pastTime - limitTime >= 0.0f;

		if( isLoop & isLimitOver ) pastTime -= limitTime;

		return isLimitOver;
		
	}

	
	public void update()
	{

		animate();

		if( drawer.mr.isVisible )
		{

			setPosturesWorld();
			//setMatrixPostures();

			//updateBones();
			drawer.update( postures );//mtPostures );
 
		}

	}


	void setMatrixPostures()
	{

		mtPostures[ streamStates.streamSpanLength ].SetTRS( act.rb.position, act.rb.rotation, act.tf.localScale );


		var pstate = streamStates[ 0, EnKeyMode.pos ];

		var tpos = ( pastTime - pstate.from.time ) * pstate.timeRate;

		var dpos =	interpolate( pstate.prev.vc3, pstate.from.vc3, pstate.to.vc3, pstate.next.vc3, Mathf.Clamp01( tpos ) );
		//Debug.Log( pstate.prev.keyCount + "/" + pstate.from.keyCount + "/" + pstate.to.keyCount + "/" + pstate.next.keyCount +" " + dpos );

		motions.streamInfos[ 0 ].localPosition = dpos + act.tfBody.localPosition;


		for( var i = 0 ; i < streamStates.streamSpanLength ; i++ )
		{

			var rstate = streamStates[ i, EnKeyMode.rot ];

			var trot = ( pastTime - rstate.from.time ) * rstate.timeRate;

			var s = Vector4.Dot( rstate.from.vc4, rstate.to.vc4 ) > 0.0f ? 1.0f : -1.0f;

			var drot = interpolate( rstate.prev.vc4, rstate.from.vc4, rstate.to.vc4 * s, rstate.next.vc4 * s, Mathf.Clamp01( trot ) ).normalized;


			var lpos = motions.streamInfos[ i ].localPosition;

			var lrot = new Quaternion( drot.x, drot.y, drot.z, drot.w );
			
			var lscale = Vector3.one;


			mtPostures[ i ].SetTRS( lpos, lrot, lscale );

			var pid = motions.streamInfos[ i ].parentId;

			mtPostures[ i ] = mtPostures[ pid ] * mtPostures[ i ];

		}

	}

	void setPostures()
	{

		postures[ streamStates.streamSpanLength ] = new PostureUnit( act.rb.position, act.rb.rotation, act.tf.localScale );


		var pstate = streamStates[ 0, EnKeyMode.pos ];

		var tpos = ( pastTime - pstate.from.time ) * pstate.timeRate;

		var dpos = pstate.from.vc3;//interpolate( pstate.prev.vc3, pstate.from.vc3, pstate.to.vc3, pstate.next.vc3, tpos > 1.0f ? 1.0f : tpos );
		//Debug.Log( pstate.prev.keyCount + "/" + pstate.from.keyCount + "/" + pstate.to.keyCount + "/" + pstate.next.keyCount +" " + dpos );

		motions.streamInfos[ 0 ].localPosition = dpos;


		for( var i = 0 ; i < streamStates.streamSpanLength ; i++ )
		{

			var rstate = streamStates[ i, EnKeyMode.rot ];

			var trot = ( pastTime - rstate.from.time ) * rstate.timeRate;

			var drot = rstate.from.vc4;//interpolate( rstate.prev.vc4, rstate.from.vc4, rstate.to.vc4, rstate.next.vc4, trot > 1.0f ? 1.0f : trot ).normalized;


			var pid = motions.streamInfos[ i ].parentId;

			var lpos = motions.streamInfos[ i ].localPosition;

			var lrot = new Quaternion( drot.x, drot.y, drot.z, drot.w );
			
			var lscale = Vector3.one;

			postures[ i ] = new PostureUnit( lpos, lrot, lscale );

		}

	}

	void setPosturesWorld()
	{


		var pstate = streamStates[ 0, EnKeyMode.pos ];

		var tpos = ( pastTime - pstate.from.time ) * pstate.timeRate;

		var dpos = pstate.from.vc3;//interpolate( pstate.prev.vc3, pstate.from.vc3, pstate.to.vc3, pstate.next.vc3, tpos > 1.0f ? 1.0f : tpos );
		//Debug.Log( pstate.prev.keyCount + "/" + pstate.from.keyCount + "/" + pstate.to.keyCount + "/" + pstate.next.keyCount +" " + dpos );

		//motions.streamInfos[ 0 ].localPosition = act.rb.rotation * dpos + act.tfBody.localPosition;

		postures[ streamStates.streamSpanLength ] = new PostureUnit( act.rb.rotation * dpos + act.tfBody.position, act.rb.rotation, act.tf.localScale );


		for( var i = 0 ; i < streamStates.streamSpanLength ; i++ )
		{

			var rstate = streamStates[ i, EnKeyMode.rot ];

			var trot = ( pastTime - rstate.from.time ) * rstate.timeRate;

			var drot = rstate.from.vc4;//interpolate( rstate.prev.vc4, rstate.from.vc4, rstate.to.vc4, rstate.next.vc4, trot > 1.0f ? 1.0f : trot ).normalized;


			var pid = motions.streamInfos[ i ].parentId;
			
			var pp = postures[ pid ];

			var lrot = pp.localRotation * new Quaternion( drot.x, drot.y, drot.z, drot.w );

			var lpos = motions.streamInfos[ i ].localPosition;

			lpos = pp.localRotation * lpos + new Vector3( pp.localPosition.x, pp.localPosition.y, pp.localPosition.z );

			var lscale = Vector3.one;

			postures[ i ] = new PostureUnit( lpos, lrot, lscale );

		}

	}
	
	void updateBones()
	// 対応するＴＦにローカル値を流し込む。
	{

		var tfBones = getTfBonesFromRenderSystem();

		for( var i = tfBones.Length; i-->0 ; )
		//for( var i = 0 ; i < tfBones.Length ; i++ )
		{

			var tfBone = tfBones[ i ];

			if( tfBone )
			{
				tfBone.localScale = postures[ i ].localScale;
				tfBone.localRotation = postures[ i ].localRotation;
				tfBone.localPosition = postures[ i ].localPosition;
			}

		}

	}






	static Vector3 interpolate( Vector3 v0, Vector3 v1, Vector3 v2, Vector3 v3, float t )
	{
		return v1 + 0.5f * t * ( ( v2 - v0 ) + ( ( 2.0f * v0 - 5.0f * v1 + 4.0f * v2 - v3 ) + ( -v0 + 3.0f * v1 - 3.0f * v2 + v3 ) * t ) * t );
	}
	static Vector4 interpolate( Vector4 v0, Vector4 v1, Vector4 v2, Vector4 v3, float t )
	{
		return v1 + 0.5f * t * ( ( v2 - v0 ) + ( ( 2.0f * v0 - 5.0f * v1 + 4.0f * v2 - v3 ) + ( -v0 + 3.0f * v1 - 3.0f * v2 + v3 ) * t ) * t );
	}



	// ワールド位置情報を保持するワークのユニット -----------------
	[System.Serializable]
	public struct PostureUnit
	{

		public Vector4		localPosition;

		public Quaternion	localRotation;

		public Vector4		localScale;

		public PostureUnit( Vector3 pos, Quaternion rot, Vector3 scale )
		{
			localPosition = pos;
			localRotation = rot;
			localScale = scale;
		}

	}




	// ----------------------------------------
	[System.Serializable]
	public
	struct StreamKeyHolder
	{
		public
		StreamKeyCursorUnit[]	streams;

		public int streamSpanLength { get; private set; }

		public int length { get { return streams.Length; } }

		public int positionLength { get { return streams.Length - streamSpanLength; } }
		public int rotationLength { get { return streamSpanLength; } }

		public bool isReady { get { return length > 0; } }


		public StreamKeyHolder( int streamLength, EnStreamMode enMode )
		{
			var rotationLength = streamLength;

			var positionLength = (int)enMode * streamLength + (int)enMode ^ 1;

			streams = new StreamKeyCursorUnit[ rotationLength + positionLength ];

			for( var irot = 0 ; irot < rotationLength ; irot++ )
			{
				streams[ irot ].init( irot, EnKeyMode.rot );
			}

			for( var ipos = 0 ; ipos < positionLength ; ipos++ )
			{
				streams[ rotationLength + ipos ].init( ipos, EnKeyMode.pos );
			}

			streamSpanLength = streamLength;
		}

		
		public StreamKeyCursorUnit this[ int streamId, EnKeyMode enKeyMode ]
		{
			get { return streams[ streamId + streamSpanLength * (int)enKeyMode ]; }
			set { streams[ streamId + streamSpanLength * (int)enKeyMode ] = value; }
		}
		/*
		public StreamKeyCursorUnit this[ int streamId ]
		{
			get { return streams[ streamId ]; }
			set { streams[ streamId ] = value; }
		}
		*/
	}
	[System.Serializable]
	public
	struct StreamKeyCursorUnit
	{
		public
		int			streamId;
		public
		EnKeyMode	enKeyMode;

		public float	timeRate;

		public KeyCursorUnit	prev;
		public KeyCursorUnit	from;
		public KeyCursorUnit	to;
		public KeyCursorUnit	next;

		public void init( int _streamId, EnKeyMode _enKeyMode )
		{
			streamId = _streamId;
			enKeyMode = _enKeyMode;
		}

		
		public void set( Motions motions, int motionId, bool isLoop )
		{

			var stream = motions[ motionId ].getStream( streamId, enKeyMode );

			prev =
			from = stream.getKey( motionId, 0 );

			if( isLoop )
			{
				to = stream.getKeyLimitedMax( motionId, 1 );//

				next = stream.getKeyLimitedMax( motionId, 2 );//
			}
			else
			{
				to = stream.getKeyLimitedMax( motionId, 1 );

				next = stream.getKeyLimitedMax( motionId, 2 );
			}


			timeRate = to.time != from.time ? 1.0f / ( to.time - from.time ) : 0.0f;

		}

		public void shiftTo( Motions motions, int motionId, bool isLoop )
		// 次のモーションにただちに遷移させる。
		// 次のモーションは最初のフレームがタイムゼロならカットする。
		{

			var stream = motions[ motionId ].getStream( streamId, enKeyMode );

			var nextKeyOffset = stream.times[ 0 ] == 0.0f ? 1 : 0;

			to = stream.getKeyLimitedMax( motionId, 0 + nextKeyOffset );

			next = stream.getKeyLimitedMax( motionId, 1 + nextKeyOffset );


			timeRate = to.time != from.time ? 1.0f / ( to.time - from.time ) : 0.0f;
			
		}


		public void progress( float time, Motions motions, bool isLoop )
		{

			while( time > to.time )
			{
				prev = from;
				from = to;
				to = next;

				if( to.keyCount > 0 )
				{
					// 次のキーへ

					var stream = motions[ to.motionId ].getStream( streamId, enKeyMode );

					next = stream.getKeyNextCount( to.motionId, to.keyCount );
					
					timeRate = to.time != from.time ? 1.0f / ( to.time - from.time ) : 0.0f;

				}
				else if( isLoop )
				{
					// 先頭キーへ

					time -= from.time;

					var stream = motions[ to.motionId ].getStream( streamId, enKeyMode );

					var nextKeyOffset = stream.times[ 0 ] == 0.0f ? 1 : 0;

					next = stream.getKeyLimitedMax( to.motionId, 0 + nextKeyOffset );

					timeRate = to.time != from.time ? 1.0f / to.time : 0.0f;

				}
				else
				{
					// 最後のキーがずっと続く

					to.time = float.PositiveInfinity;

				}

				//if( streamId == 1 ) Debug.Log( streamId + " " + prev.keyCount + " " + from.keyCount + " " + to.keyCount + " " + next.keyCount );

			}

		}

	}
	[System.Serializable]
	public
	struct KeyCursorUnit
	{
		public int		motionId;
		public int		keyCount;
		public float	time;
		public Vector4	value;

		public KeyCursorUnit( int mId, int counter, float t, Vector4 vc )
		{
			motionId = mId;
			keyCount = counter;
			time = t;
			value = vc;
		}

		public Vector3 vc3 { get { return (Vector3)value; } }
		public Vector4 vc4 { get { return value; } }
		public Quaternion qt { get { return new Quaternion( value.x, value.y, value.z, value.w ); } }
	}


	// ----------------------------------------------

	[System.Serializable]
	struct BoneLinker
	{

		public TransformLinkUnit[] tfs;
		public RigidbodyLinkUnit[] rbs;
		public LocalScalLinkUnit[] localScales;


		public void scanLinks( Transform[] tfBones, Transform[] tfNotRemoves )
		{
			var lsf = new LocalScaleFinder( tfBones.Length );
			var rbf = new RididbodyLinkFinder( tfBones.Length );
			var tff = new TransformLinkFinder( tfBones.Length );

			for( var i = 0 ; i < tfBones.Length ; i++ )
			{
				if( tfBones[i] )
				{

					var tf = tfBones[ i ];

					lsf.check( tf, i );

					var isFoundRb = rbf.check( tf, i );

					if( !isFoundRb )
					{
						tff.check( tf, i, tfNotRemoves );
					}
					
				}
			}

			tfs = tff.toArray();
			rbs = rbf.toArray();
			localScales = lsf.toArray();
		}


		struct LocalScaleFinder
		{

			List<LocalScalLinkUnit>	links;

			public LocalScaleFinder( int boneLength )
			{
				links = new List<LocalScalLinkUnit>( boneLength );
			}

			public void check( Transform tf, int i )
			{
				var ls = tf.localScale;

				if( ls != Vector3.one )
				{
					var link = new LocalScalLinkUnit();

					link.id = i;
					link.localScale = tf.localScale;

					links.Add( link );
				}
			}

			public LocalScalLinkUnit[] toArray()
			{
				return links.ToArray();
			}
		}

		struct RididbodyLinkFinder
		{

			List<RigidbodyLinkUnit>	links;

			public RididbodyLinkFinder( int boneLength )
			{
				links = new List<RigidbodyLinkUnit>( boneLength );
			}

			public bool check( Transform tf, int i )
			{
				var rb = tf.GetComponent<Rigidbody>();

				if( rb )
				{
					var link = new RigidbodyLinkUnit();

					link.id = i;
					link.rb = rb;

					links.Add( link );
				}

				return rb != null;
			}

			public RigidbodyLinkUnit[] toArray()
			{
				return links.ToArray();
			}
		}

		struct TransformLinkFinder
		{

			List<TransformLinkUnit>	links;

			public TransformLinkFinder( int boneLength )
			{
				links = new List<TransformLinkUnit>( boneLength );
			}

			public void check( Transform tf, int i, Transform[] tfNotRemoves )
			{
				if( findInList( tf, tfNotRemoves ) || tf.GetComponent<Collider>() )
				{
					var link = new TransformLinkUnit();

					link.id = i;
					link.tf = tf;

					links.Add( link );
				}
			}

			bool findInList( Transform tf, Transform[] tfs )
			{
				foreach( var itf in tfs ) if( tf == itf ) return true;

				return false;
			}

			public TransformLinkUnit[] toArray()
			{
				return links.ToArray();
			}
		}




	}

	[System.Serializable]
	public struct TransformLinkUnit
	{
		public int			id;
		public Transform	tf;
	}

	[System.Serializable]
	public struct RigidbodyLinkUnit
	{
		public int			id;
		public Rigidbody	rb;
	}

	[System.Serializable]
	public struct LocalScalLinkUnit
	{
		public int		id;
		public Vector3	localScale;
	}


}


/*
public abstract class _PostureWork
{

	
	

	

}

public class PostureMatrixWork : _PostureWork
{

	struct PostureMatrixUnit
	{

		public Matrix4x4	mt;

	}

}

public class PosturePositionRotationWork : _PostureWork
{

	struct PostureUnit
	{

		public Vector4		position;

		public Quaternion	rotation;

	}
	void transform()
	{

		postures[ streamStates.streamSpanLength ].position = act.rb.position;

		postures[ streamStates.streamSpanLength ].rotation = act.rb.rotation;


		var pstate = streamStates[ 0, EnKeyMode.pos ];

		var tpos = ( pastTime - pstate.from.time ) * pstate.timeScaleR;

		var dpos = interpolate( pstate.prev.vc3, pstate.from.vc3, pstate.to.vc3, pstate.next.vc3, tpos );

		motions.streamInfos[ 0 ].localPosition = dpos;


		for( var i = 0 ; i < streamStates.streamSpanLength ; i++ )
		{

			var rstate = streamStates[ i, EnKeyMode.rot ];

			var trot = ( pastTime - rstate.from.time ) * rstate.timeScaleR;

			var drot = interpolate( rstate.prev.vc4, rstate.from.vc4, rstate.to.vc4, rstate.next.vc4, trot );


			var pid = motions.streamInfos[ i ].parentId;

			postures[ i ].position = (Vector3)postures[ pid ].position + postures[ pid ].rotation * motions.streamInfos[ i ].localPosition;

			postures[ i ].rotation = postures[ pid ].rotation * new Quaternion( drot.x, drot.y, drot.z, drot.w );
			
		}

}
*/

