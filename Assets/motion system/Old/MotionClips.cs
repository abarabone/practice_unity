using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR
using UnityEditor;
using System.IO;
#endif



public class MotionClips : ScriptableObject
{

	[SerializeField]
	string[]	streamPaths;

	[SerializeField]
	Clip[]		clips;


	Dictionary<Type, Motions>	compliantMotions;



	public string[] getStreamPaths()
	{
		return streamPaths;
	}

	public Clip[] getMotionClips()
	{
		return clips;
	}



	public Motions getMotions( Type type, Type EnTypeMotion, Transform[] tfBones )
	{

		if( compliantMotions == null )
		{
			compliantMotions = new Dictionary<Type, Motions>( 1 );
		}

		if( !compliantMotions.ContainsKey( type ) )
		{
			compliantMotions[ type ] = reform( EnTypeMotion, tfBones );
		}

		return compliantMotions[ type ];

	}

	Motions reform( Type EnTypeMotion, Transform[] tfBones )
	{

		var motions = new Motions();

		motions.map( this, EnTypeMotion, tfBones );

		return motions;

	}






	[ System.Serializable ]
	public struct Clip
	{

		public string	name;

		public float	time;


		public KeyStreamUnit[]	rotationStreams;

		public KeyStreamUnit[]	positionStreams;

	}





	[System.Serializable]
	public struct KeyStreamUnit
	{

		public float[]		times;

		[Compact]
		public Vector4[]	keys;


		public MotionPlayer.KeyCursorUnit getKey( int motionId, int keyId )
		{
			return new MotionPlayer.KeyCursorUnit( motionId, keys.Length - ( keyId + 1 ), times[ keyId ], keys[ keyId ] );
		}

		public MotionPlayer.KeyCursorUnit getKeyLimitedMax( int motionId, int keyId )
		{
			if( keyId >= keys.Length ) keyId = keys.Length - 1;// Debug.Log( keys.Length );

			return new MotionPlayer.KeyCursorUnit( motionId, keys.Length - ( keyId + 1 ), times[ keyId ], keys[ keyId ] );
		}

		public MotionPlayer.KeyCursorUnit getKeyLimitedMin( int motionId, int keyId )
		{
			keyId &= ~( keyId >> 31 );//if( keyId < 0 ) id = 0;

			return new MotionPlayer.KeyCursorUnit( motionId, keys.Length - ( keyId + 1 ), times[ keyId ], keys[ keyId ] );
		}

		public MotionPlayer.KeyCursorUnit getKeyNextCount( int motionId, int prevKeyCount )
		{
			var keyId = keys.Length - prevKeyCount;

			return new MotionPlayer.KeyCursorUnit( motionId, prevKeyCount - 1, times[ keyId ], keys[ keyId ] );
		}

	}






#if UNITY_EDITOR

	[MenuItem( "Assets/Create MotionClips" )]
	static public void Create()
	{
		foreach( var obj in Selection.objects )
		{
			if( obj.GetType() == typeof( GameObject ) )
			{
				var motionClips = new MotionClips();

				motionClips.convert( (GameObject)obj );

				motionClips.save( obj );
			}
		}
		/*
		var sel = Selection.GetFiltered( typeof( AnimationClip ), SelectionMode.Assets );

		foreach( var clip in sel )
		{
			Debug.Log( clip.name );
		}*/
	}



	void save( UnityEngine.Object obj )
	// スクリプトと同じアセット階層にある、データ保存用オブジェクトを取得する。
	{

		var editorPath = AssetDatabase.GetAssetPath( obj );

		var folderPath = Path.GetDirectoryName( editorPath );

		var objectPath = folderPath + "/Motion " + obj.name + ".asset";


		AssetDatabase.CreateAsset( this, objectPath );


		ScriptableObject sobj = (ScriptableObject)AssetDatabase.LoadAssetAtPath( objectPath, typeof( ScriptableObject ) );

		string[] labels = { "Data", "ScriptableObject" };

		AssetDatabase.SetLabels( sobj, labels );

		EditorUtility.SetDirty( sobj );

	}

	public void convert( GameObject fbx )
	{

		var contents = AssetDatabase.LoadAllAssetsAtPath( AssetDatabase.GetAssetPath(fbx) );

		var clipList = getAnimationClipList( contents );

		if( clipList.Count == 0 ) return;


		streamPaths = getPaths( clipList[0] );

		
		clips = new Clip[ clipList.Count ];

		for( var i = 0 ; i < clipList.Count ; i++ )
		{
			clips[i] = convert( clipList[i] );
		}
		
	}

	Clip convert( AnimationClip aniclip )
	{
		var curves = AnimationUtility.GetAllCurves( aniclip );

		var streams = getStreams( curves );

		var clip = setStreams( streams );

		clip.name = aniclip.name;
		clip.time = aniclip.length;

		return clip;
	}

	Clip setStreams( Dictionary<string, ClipStreamUnit> streams )
	{
		var clip = new Clip();
		
		clip.rotationStreams = new KeyStreamUnit[ streamPaths.Length ];
		clip.positionStreams = new KeyStreamUnit[ streamPaths.Length ];

		for( var i = 0 ; i < streamPaths.Length ; i++ )
		{
			var stream = streams[ streamPaths[i] ];

			clip.positionStreams[ i ].times = new float[ stream.posx.Length ];
			clip.positionStreams[ i ].keys = new Vector4[ stream.posx.Length ];

			for( var j = 0 ; j < stream.posx.Length ; j++ )
			{
				clip.positionStreams[ i ].times[ j ] = stream.posx[ j ].time;
				clip.positionStreams[ i ].keys[ j ].x = stream.posx[ j ].value;
				clip.positionStreams[ i ].keys[ j ].y = stream.posy[ j ].value;
				clip.positionStreams[ i ].keys[ j ].z = stream.posz[ j ].value;
			}

			clip.rotationStreams[ i ].times = new float[ stream.rotx.Length ];
			clip.rotationStreams[ i ].keys = new Vector4[ stream.rotx.Length ];

			for( var j = 0 ; j < stream.rotx.Length ; j++ )
			{
				clip.rotationStreams[ i ].times[ j ] = stream.rotx[ j ].time;
				clip.rotationStreams[ i ].keys[ j ].x = stream.rotx[ j ].value;
				clip.rotationStreams[ i ].keys[ j ].y = stream.roty[ j ].value;
				clip.rotationStreams[ i ].keys[ j ].z = stream.rotz[ j ].value;
				clip.rotationStreams[ i ].keys[ j ].w = stream.rotw[ j ].value;
			}
		}

		return clip;
	}

	Dictionary<string, ClipStreamUnit> getStreams( AnimationClipCurveData[] curves )
	{
		var streams = new Dictionary<string, ClipStreamUnit>( streamPaths.Length );

		foreach( var curve in curves )
		{

			if( !streams.ContainsKey( curve.path ) )
			{
				streams[ curve.path ] = new ClipStreamUnit();
			}

			var stream = streams[ curve.path ];

			switch( curve.propertyName )
			{
				case "m_LocalPosition.x": stream.posx = curve.curve.keys; break;
				case "m_LocalPosition.y": stream.posy = curve.curve.keys; break;
				case "m_LocalPosition.z": stream.posz = curve.curve.keys; break;

				case "m_LocalRotation.x": stream.rotx = curve.curve.keys; break;
				case "m_LocalRotation.y": stream.roty = curve.curve.keys; break;
				case "m_LocalRotation.z": stream.rotz = curve.curve.keys; break;
				case "m_LocalRotation.w": stream.rotw = curve.curve.keys; break;
			}

			streams[ curve.path ] = stream;

		}

		return streams;
	}

	string[] getPaths( AnimationClip aniclip )
	{

		var curves = AnimationUtility.GetAllCurves( aniclip, false );


		var paths = new HashSet<string>();

		foreach( var cv in curves )
		{
			paths.Add( cv.path );
		}

		var res = new string[ paths.Count ];

		paths.CopyTo( res );

		return res;
	}

	List<AnimationClip> getAnimationClipList( UnityEngine.Object[] contents )
	{
		var clipList = new List<AnimationClip>();

		foreach( var item in contents )
		{
			var clip = item as AnimationClip;

			if( clip != null && clip.name.IndexOf("__preview__") == -1 )
			{
				clipList.Add( clip );
			}
		}

		return clipList;
	}

	struct ClipStreamUnit
	{
		public Keyframe[]	posx;
		public Keyframe[]	posy;
		public Keyframe[]	posz;

		public Keyframe[]	rotx;
		public Keyframe[]	roty;
		public Keyframe[]	rotz;
		public Keyframe[]	rotw;
	}

#endif

}









//public class Motions
public struct Motions
{

	MotionClips.Clip[]	clips;

	IdMapper	motionMapper;

	IdMapper	streamMapper;

	
	public BoneInfo.BoneInfoUnit[] streamInfos { get; private set; }
	

	public int motionLength { get { return motionMapper.length; } }

	public int streamLength	{ get { return streamMapper.length; } }



	
	public Motion this[ int motionId ]
	{
		get { return new Motion( clips[ motionMapper[motionId] ], streamMapper ); }
	}



	public void map( MotionClips motionClips, Type EnOuterMoitions, Transform[] tfBones )
	{

		clips = motionClips.getMotionClips();

		motionMapper.mapClips( EnOuterMoitions, motionClips.getMotionClips() );


		var boneMapper = new BoneInfo();

		streamMapper.mapBones( boneMapper.toPaths( tfBones ), motionClips.getStreamPaths() );

		streamInfos = boneMapper.infos;

	}





	public struct Motion
	// 外部から Motions にモーション単体へのアクセス要求があるたびに生成する。
	{

		MotionClips.Clip	clip;

		IdMapper			mapper;


		public Motion( MotionClips.Clip motionClip, IdMapper streamMapper )
		{

			clip = motionClip;

			mapper = streamMapper;

		}
		public string name { get { return clip.name;	} }

		public float time { get { return clip.time; } }

		public int streamLength { get { return mapper.length; } }

		
		public MotionClips.KeyStreamUnit getRotationStream( int streamId )
		{
			return clip.rotationStreams[ mapper[ streamId ] ];
		}

		public MotionClips.KeyStreamUnit getPositionStream( int streamId )
		{
			return clip.positionStreams[ mapper[ streamId ] ];
		}

		public MotionClips.KeyStreamUnit getStream( int streamId, MotionPlayer.EnKeyMode enKeyMode )
		{
			return enKeyMode == MotionPlayer.EnKeyMode.pos ? getPositionStream(streamId) : getRotationStream(streamId) ;
		}

	}




	public struct IdMapper
	{

		int[]	ids;


		public int length { get { return ids.Length; } }


		public int this[ int outerIndex ]
		{
			get { return ids[ outerIndex ]; }
		}


		public void mapBones( string[] outerNames, string[] innerNames )
		{
			mapNames( outerNames, innerNames );
			Debug.Log( innerNames[ 0 ] + " " + innerNames[ 1 ] + " " + innerNames[ 2 ] + " " + innerNames[ 3 ] + " " + innerNames[ 4 ] + " " + innerNames[ 5 ] + " " );
			Debug.Log( outerNames[ 0 ] + " " + outerNames[ 1 ] + " " + outerNames[ 2 ] + " " + outerNames[ 3 ] + " " + outerNames[ 4 ] + " " + outerNames[ 5 ] + " " );
		}


		public void mapClips( Type EnOuterMoitions, MotionClips.Clip[] innerClips )
		{
			var innerNames = new string[ innerClips.Length ];

			for( var i = 0 ; i < innerClips.Length ; i++ )
			{
				innerNames[ i ] = innerClips[ i ].name;
			}

			mapNames( Enum.GetNames( EnOuterMoitions ), innerNames );
		}


		void mapNames( string[] outerNames, string[] innerNames )
		{
			var innerIds = createInnerList( innerNames );

			ids = new int[ outerNames.Length ];

			for( var i = 0 ; i < outerNames.Length ; i++ )
			{
				if( innerIds.ContainsKey( outerNames[ i ] ) )
				{
					ids[ i ] = innerIds[ outerNames[ i ] ];
				}
			}
		}

		static public Dictionary<string, int> createInnerList( string[] innerNames )
		{
			var innerIds = new Dictionary<string, int>( innerNames.Length );

			for( var i = 0 ; i < innerNames.Length ; i++ )
			{
				innerIds[ innerNames[ i ] ] = i;
			}

			return innerIds;
		}

	}




	public struct BoneInfo
	{

		StringBuilder	path;

		List<string>	pathList;

		List<BoneInfoUnit>	infoList;

		public BoneInfoUnit[] infos { get; private set; }



		// トップレベルのボーンから再帰的に構築する ----------------

		public string[] toPaths( Transform tfBase )
		{

			pathList = new List<string>();

			infoList = new List<BoneInfoUnit>();


			path = new StringBuilder( 256 );

			setPath( tfBase, -1 );

			setRootId();


			infos = infoList.ToArray();

			return pathList.ToArray();
		}

		void setPath( Transform tfBone, int parentId )
		{

			var lp = path.Length;


			pathList.Add( path.Append( tfBone.name ).ToString() );

			infoList.Add( new BoneInfoUnit( tfBone.localPosition, parentId ) );


			if( tfBone.childCount > 0 )
			{
				path.Append( "/" );

				parentId = infoList.Count - 1;

				for( int i = 0 ; i < tfBone.childCount ; i++ )
				{
					setPath( tfBone.GetChild( i ), parentId );
				}
			}


			path.Length = lp;

		}

		void setRootId()
		{
			for( var i = 0 ; i < infoList.Count ; i++ )
			{
				if( infoList[ i ].parentId == -1 )
				{
					var info = infoList[ i ];
					
					info.parentId = infoList.Count;

					infoList[ i ] = info;
				}
			}
		}


		// ＴＦの配列から構築する -------------------

		public string[] toPaths( Transform[] tfBones )
		{

			pathList = new List<string>();

			infoList = new List<BoneInfoUnit>();


			var tfBody = tfBones[ 0 ].findInParents( "body" );


			path = new StringBuilder( 256 );

			foreach( var tfBone in tfBones )
			{
				if( !tfBone ) continue;

				setPath( tfBone, tfBody );

				setInfo( tfBone, tfBones );

				//Debug.Log( ( pathList.Count - 1 ) + ":" + pathList[ pathList.Count - 1 ] + ":" + infoList[ pathList.Count - 1 ].parentId + "/" + infoList[ pathList.Count - 1 ].localPosition );
			}


			infos = infoList.ToArray();

			return pathList.ToArray();
		}

		void setPath( Transform tfBone, Transform tfBody )
		{
			
			path.Length = 0;// Debug.Log( tfBone.name );

			path.Append( tfBone.name );

			for( var tfThis = tfBone.parent; tfThis != tfBody ; tfThis = tfThis.parent )
			{
				path.Insert( 0, "/" ).Insert( 0, tfThis.name );
			}

			pathList.Add( path.ToString() );

		}

		void setInfo( Transform tfBone, Transform[] tfBones )
		{

			for( var i = 0 ; i < infoList.Count ; i++ )
			{
				if( tfBone.parent == tfBones[ i ] )
				{
					infoList.Add( new BoneInfoUnit( tfBone.localPosition, i ) );

					return;
				}
			}

			infoList.Add( new BoneInfoUnit( tfBone.localPosition, tfBones.Length ) );

		}



		// --------------------------------

		public struct BoneInfoUnit
		{
			public Vector3	localPosition;
			public int		parentId;	// ベースノードでは最後部（全てのボーンの後）を指す。最後部ノードはルートを司る。

			public BoneInfoUnit( Vector3 pos, int pid )
			{
				localPosition = pos;
				parentId = pid;
			}
		}

	}

}



