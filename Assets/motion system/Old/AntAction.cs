using UnityEngine;
using System.Collections;

#if false

public class AntAction : _Action3Enemy
{
	
	//public float attackTiming	= 0.06f;	// 0.00f ～ 0.07f の範囲で遅延可能、回避のしやすいタイミングで！

	protected float	ratedAttackTiming;



	//protected MotionStateHolder	ms;

	public WriggleUnit			wriggler;



	//MotionFader		motion;
	public MotionPlayer	motion;


	enum enMotion
	{
		standing,

		walking,

		attack02,

		damage,

		dead
	}





	// 初期化 ------------------------------------------

	//protected override void deepInit()
	//{

		//base.deepInit();

	//}


	public override void init()
	{

		base.init();

		motion.buildLite( this, typeof( AntAction ), typeof( enMotion ) );


		state.init( stay );

		motion.init( 0 );

		tfBody.GetChild( 0 ).DetachChildren();//


		wriggler.init( this );


		posture.fittingRate = 3.0f;


		ratedAttackTiming = def.attackTiming / def.quickness * figure.scaleRateR;

		//rb.inertiaTensor = rb.inertiaTensor;
		//rb.inertiaTensorRotation = rb.inertiaTensorRotation;
	}



	// 更新処理 ----------------------------------------

	new protected void Update()
	{

		shoot.reload( 0 );//暫定

		base.Update();


		motion.update();

		if( !isDead )
		{

			//wriggler.update( this );

		}

	}









	// モード変更 --------------------------------------

	public override void changeToWaitMode()
	{

		finder.target.clear();

		mode.poll( ref finder.target );

		migration.setTimer( Random.value * 3.0f );

	}

	public override void changeToAttackMode( _Action3 attacker )//= null )
	{

		if( attacker == null ) return;

		if( attacker == this )
		{
			changeToAlertMode();

			return;
		}


		finder.target.toDecision( character, attacker );

		mode.poll( ref finder.target );

		migration.setTimer( Random.value * 3.0f );

	}

	public override void changeToAlertMode()
	{

		finder.target.toAlert( character );

		mode.poll( ref finder.target );

		migration.setTimer( Random.value * 3.0f );

	}

	public override void changeToDamageMode( _Action3 attacker, float time )
	{

		changeToAttackMode( attacker );


		if( figure.scaleRate >= 3.0f )
		{
			// スーパーアーマー
		}
		else
		{

			migration.setTimer( time );

			state.changeTo( damage );

		}

	}

	public override void changeToBlowingMode( _Action3 attacker, float time, int level )
	{

		changeToAttackMode( attacker );


		if( figure.scaleRate >= 3.0f )
		{
			// スーパーアーマー
		}
		else
		{

			state.setPhysics( physFree );

			shiftToPhysicalMode();//


			migration.setTimer( time );

			state.changeTo( blow );

		}

	}

	public override void changeToDeadMode()
	{

		finder.target.clear();

		state.changeTo( dead );

	}



	// アクション ================================================================================

	// stay ----------------------------------------------------

	void stay( ref ActionStateUnit.ActionHolder action )
	{

		action.first = () =>
		{

			state.shiftTo( staying );

			state.setPhysics( physStayOnWall );
			posture.setCollision( collisionWallInStay );


			migration.setTimer( Random.Range( 1.0f, 6.0f ) );

			//moveSpeed.set( 0.0f );// = quickness * 1.0f;


			wriggler.full();

			//motion.fadeIn( ms.stand, 0.3f );
			motion.shiftTo( (int)enMotion.standing, true );

			//ms.stand.speed = def.quickness * figure.scaleRateR;
			motion.motionSpeed = def.quickness * figure.scaleRateR;

		};

		action.last = () =>
		{

		};

	}

	void staying()
	{

		move.lerp( 0.0f, 3.0f );


		if( mode.isBattling | mode.isDoubtEnemy ) finder.search( 0, 1 ); else finder.search( 1 );


		var preMode = mode;

		switch( mode.poll( ref finder.target ) )
		{

			case BattleState.EnMode.doubt:

				migration.setTargetPoint( this, ref finder.target );

				var isFront = lookAtTarget( 1.0f, 40.0f );

				if( !isFront ) state.changeTo( walk );

				break;


			case BattleState.EnMode.lost:
			case BattleState.EnMode.alert:
			case BattleState.EnMode.weary:
			case BattleState.EnMode.wait:

				if( migration.isLimitOver )
				{

					if( isGround() )
					{
						state.changeTo( walk );
					}
					else
					{
						//motion.fadeIn( ms.walk, 0.5f );
						motion.shiftTo( (int)enMotion.walking, true );
					}

				}

				break;


			case BattleState.EnMode.decision:

				state.changeTo( walk );

				//if( !preMode.isChaseTrigger( ref finder.target ) )
				if( !preMode.isChaseEnemy )
				{
					//Debug.Log( "decision" );
					connection.notifyAttack( this, finder.target.act );

					migration.resetTimer();// すぐに追いかけ始めるように
				}

				break;

		}


		output.loudness = 0.3f;

	}


	// walk ----------------------------------------------------

	void walk( ref ActionStateUnit.ActionHolder action )
	{

		action.first = () =>
		{

			state.shiftTo( walking );


			state.setPhysics( physMoveOnWall );
			posture.setCollision( collisionWallInMove );


			if( mode.isWait )
			{
				migration.setTargetPoint( this );
			}


			wriggler.full();

			//motion.fadeIn( ms.walk, 0.3f );
			motion.shiftTo( (int)enMotion.walking, true );

		};

		action.last = () =>
		{

		};

	}

	void walking()
	{

		if( mode.isBattling | mode.isDoubtEnemy ) finder.search( 0, 1 ); else finder.search( 0 );


		var preMode = mode;

		switch( mode.poll( ref finder.target ) )
		{

			case BattleState.EnMode.doubt:
				{

					if( preMode.isChaseEnemy )
					{

						state.changeTo( stay );

					}
					else
					{
						migration.setTargetPoint( this, ref finder.target );

						var isFront = lookAtTarget( 2.0f, 30.0f );

						if( isFront ) state.changeTo( stay );
					}

				}
				break;


			case BattleState.EnMode.lost:
			case BattleState.EnMode.alert:
				{

					migration.routine( this, ref finder.target );

					lookAtTarget();

				}
				break;


			case BattleState.EnMode.wait:
				{

					var isGoal = migration.isGoalOrLimit( ref figure, rb.position );

					if( isGoal )
					{
						state.changeTo( stay );

						break;
					}

					lookAtTarget();

				}
				break;


			case BattleState.EnMode.weary:
			case BattleState.EnMode.decision:
				{

					//if( !preMode.isChaseTrigger( ref finder.target ) )
					if( !preMode.isChaseEnemy )
					{
						//Debug.Log( "decision" );
						connection.notifyAttack( this, finder.target.act );

						migration.resetTimer();// すぐに追いかけ始めるように
					}


					migration.routine( this, ref finder.target );

					var isFront = lookAtTarget( 3.0f, 120.0f );


					if( isFront && finder.target.isReach( def.reach, ref finder ) )
					{

						if( shoot.isReady( 0, 0 ) )
						{

							state.changeTo( attack );

						}
						else
						{
							if( figure.scaleRate < 3.0f )//&& finder.target.isReach( figure.bodyRadius * 3.0f, this ) )//moves.isGoal( figure.moveCastRadius * 3.0f, rb.position ) )
							{

								// 近くに寄りすぎた場合はターンする。
								turnDirection( 10.0f, Random.Range( 2.0f, 5.0f ) );

								finder.target.toLost( character );

							}
						}

					}

				}
				break;

		}




		//ms.walk.speed = move.speed * figure.scaleRateR;
		motion.motionSpeed = move.speed * figure.scaleRateR;
		//Debug.Log( move.velocity );

		var modeSpeed = finder.target.isRelease ? 1.0f : 2.0f;

		move.lerp( modeSpeed * def.quickness, 3.0f );


		output.loudness = move.speed;

	}


	// attack ----------------------------------------------------

	void attack( ref ActionStateUnit.ActionHolder action )
	{

		action.first = () =>
		{

			state.shiftTo( attacking );


			//state.setPhysics( physStayOnWall );
			//setPhysicsStayMode();

			posture.setCollision( collisionWallInStay );


			//moveSpeed.set( 0.0f );


			wriggler.turnOnly();

			//motion.fadeIn( ms.attack, 0.3f );
			motion.shiftTo( (int)enMotion.attack02 );

			//ms.attack.speed = def.quickness * figure.scaleRateR;
			motion.motionSpeed = def.quickness * figure.scaleRateR;

		};

		action.last = () =>
		{

		};

	}


	void attacking()
	{

		move.lerp( 0.0f, 3.0f );


		if( motion.time < 0.2f )//ms.attack.length * 0.5f )
		{

			if( finder.search( 0 ) )
			{

				//moves.targetPoint = finder.target.imaginaryPosition;//.act.rb.position;
				migration.setTargetPoint( this, ref finder.target );

				lookAtTarget( 1.0f, 30.0f, true );

			}

		}

		if( motion.time >= ratedAttackTiming & !shoot.weapons[ 0 ].isReloading )
		{

			shootTarget( 0, 0, 30.0f );

		}


		if( motion.isFinished )
		{

			state.changeTo( walk );

			if( character.aggressive >= CharacterInfo.EnAggressive.low )
			{
				finder.target.toLost( character );
			}

		}


		output.loudness = 2.0f;

	}



	// damaging ----------------------------------------------------

	void damage( ref ActionStateUnit.ActionHolder action )
	{

		action.first = () =>
		{

			state.shiftTo( damaging );

			//state.setPhysics( physStayOnWall );
			//setPhysicsStayMode();
			//shiftToPhysicalMode();

			posture.setCollision( collisionWallInStay );


			wriggler.none();

			//motion.fadeIn( ms.damage, 0.2f );
			motion.shiftTo( (int)enMotion.damage, true );

			//ms.damage.speed = 2.0f;//def.quickness * figure.scaleRateR;
			motion.motionSpeed = 2.0f;

		};

		action.last = () =>
		{

			//shiftToPhysicalMode();

		};

	}

	void damaging()
	{

		move.lerp( 0.0f, 5.0f );


		if( migration.isLimitOver )
		{

			state.changeTo( walk );

		}


		output.loudness = 2.0f;

	}



	// blowing ----------------------------------------------------

	void blow( ref ActionStateUnit.ActionHolder action )
	{

		action.first = () =>
		{

			state.shiftTo( browing );


			state.setPhysics( physFree );
			shiftToPhysicalMode();
			posture.setCollision( null );


			wriggler.none();

			//motion.fadeIn( ms.walk, 0.2f );//ms.damage, 0.2f );
			motion.shiftTo( (int)enMotion.walking, true );

			//ms.damage.speed = def.quickness * figure.scaleRateR;
			motion.motionSpeed = def.quickness * figure.scaleRateR;


			rb.constraints = RigidbodyConstraints.None;

		};

		action.last = () =>
		{

			rb.constraints = figure.rbDefaultConstraints;

			//shiftToPhysicalMode();

		};

	}

	void browing()
	{

		move.lerp( 0.0f, 5.0f );


		if( migration.isLimitOver )
		{

			state.changeTo( walk );

		}


		output.loudness = 2.0f;

	}



	// dead -------------------------------------------------------

	void dead( ref ActionStateUnit.ActionHolder action )
	{

		action.first = () =>
		{

			state.shiftTo( deadingStay );


			state.setPhysics( physFree );
			shiftToPhysicalMode();
			posture.setCollision( null );


			move.velocity = 0.0f;


			wriggler.turnOnly();

			//motion.fadeIn( ms.dead, 0.3f );
			motion.shiftTo( (int)enMotion.dead, true );


			migration.setTimer( 30.0f );


			rb.constraints = RigidbodyConstraints.None;



			figure.moveCollider.enabled = false;

			rb.isKinematic = false;


			switchEnvelopeDeadOrArive( UserLayer._enemyEnvelope, UserLayer._enemyEnvelopeDead );


			deadize();

		};

		action.last = () =>
		{

			//shiftToPhysicalMode();


			rb.detectCollisions = true;

			switchEnvelopeDeadOrArive( UserLayer._enemyEnvelopeDead, UserLayer._enemyEnvelope );

			rb.constraints = figure.rbDefaultConstraints;

			//shiftToPhysicalMode();


			final();

		};

	}

	void deadingStay()
	{

		if( migration.isLimitOver )
		{

			state.shiftTo( deadingFall );

			migration.setTimer( 3.0f );

			rb.detectCollisions = false;

		}

	}

	void deadingFall()
	{

		if( migration.isLimitOver )
		{

			state.changeToIdle();

		}

	}


	protected void switchEnvelopeDeadOrArive( int oldLayer, int newLayer )
	{

		var tfEnv = tfBody.findWithLayerInDirectChildren( oldLayer );
		//var tfEnv = tf.findWithLayerInDirectChildren( oldLayer );

		if( tfEnv != null )
		{

			var cs = tfEnv.GetComponents<Collider>();

			cs[ 1 ].enabled = !cs[ 1 ].enabled;

			cs[ 0 ].enabled = !cs[ 0 ].enabled;

			tfEnv.gameObject.layer = newLayer;

		}

	}





	// --------------------------------------

	[System.Serializable]
	public struct WriggleUnit
	{

		public AnimationState	bend;

		public AnimationState	turn;


		public Transform	tfHead;

		public Transform	tfWeist;

		Quaternion	preRotBody;// ターン表現のために必要


		public void deepInit( Animation anim )
		{
			/*
			turn = anim[ "turn02LR" ];
			turn.AddMixingTransform( tfHead );
			turn.AddMixingTransform( tfWeist );
			turn.layer = 1;
			turn.blendMode = AnimationBlendMode.Additive;
			turn.speed = 0.0f;
			turn.weight = 1.0f;

			bend = anim[ "turn02UD+" ];
			bend.AddMixingTransform( tfHead );
			bend.AddMixingTransform( tfWeist );
			bend.layer = 2;
			bend.blendMode = AnimationBlendMode.Additive;
			bend.speed = 0.0f;
			bend.weight = 1.0f;
			*/
		}

		public void init( _Action3Enemy act )
		{

			//preRotBody = act.rb.rotation;

		}


		public void full()
		{
			//turn.enabled = true;

			//bend.enabled = true;
		}

		public void none()
		{
			//turn.enabled = false;

			//bend.enabled = false;
		}

		public void bendOnly()
		{
			//turn.enabled = false;

			//bend.enabled = true;
		}

		public void turnOnly()
		{
			//turn.enabled = true;

			//bend.enabled = false;
		}


		public void update( _Action3Enemy act )
		{
			/*
			var rotBody = act.rb.rotation;


			if( turn.enabled || bend.enabled )
			{

				var rotWriggle = Quaternion.Inverse( preRotBody ) * rotBody;

				var angles = rotWriggle.eulerAngles;


				if( turn.enabled )
				{
					var turnAngle = ( angles.y > 180.0f ? angles.y - 360.0f : angles.y ) * GM.t.deltaR;

					turnAngle = Mathf.Clamp( turnAngle, -60.0f, 60.0f );

					var turnTime = turnAngle * ( -1.0f / 60.0f ) + 1.0f + 1.0f;

					turn.time = Mathf.Lerp( turn.time, turnTime, GM.t.delta * act.move.speed * 3.0f );
				}


				if( bend.enabled )
				{
					var bendAngle = ( angles.x > 180.0f ? angles.x - 360.0f : angles.x ) * GM.t.deltaR;

					//	bendAngle = Mathf.Clamp( bendAngle, -90.0f, 90.0f );

					var bendTime = bendAngle * ( -1.0f / 90.0f ) + 2.0f + 1.0f;

					bend.time = Mathf.Lerp( bend.time, bendTime, GM.t.delta * act.move.speed * 3.0f );
				}

			}


			preRotBody = rotBody;
			*/
		}
	}

}

#endif