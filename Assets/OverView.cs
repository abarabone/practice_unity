using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace PracticeUnity
{
	
	public interface IPoolableObject
	{
		void PassPool();
		void Back();
	}
	public interface IObjectPool
	{
		IPoolableObject Rent();
	}

	public class GameObjectPool : IObjectPool
	{
		private List<IPoolableObject>	objects;

		public IPoolableObject Rent()
		{
			return null;
		}
	}
	

	public interface ICharacterObject
	{
		
	}
	public class CharacterObject
	{
		
	}
	public interface ICharacterActionModel
	{
		
	}
	public class PlayerLocalActionModel : ICharacterActionModel
	{ 
		
	}
	public class PlayerRemoteActionModel : ICharacterActionModel
	{ 
		
	}

	public interface IStructureObject
	{

	}
	public interface IPartObject
	{ 
		
	}


	public class Building : IStructureObject
	{ 
		
	}
	public class PlotField : IStructureObject
	{

	}
	public class RoadField : IStructureObject
	{

	}
	public interface IHitTarget
	{
		
	}

	
	public class WideArea : IStructureObject
	{ 
		
	}
	public class StructureBinder : IStructureObject
	{ 
		
	}
	

	public interface IWapon
	{ 
		void Switch();
		void Select( uint id );
	}
	public interface IFireUnit
	{ 
		void Fire();
		void Reload();
	}
	public interface ITriggerUnit
	{ 
		void Pulse();
		void Bulk( uint freq );
		void Single();
	}
	
	public interface IEmitable
	{
		
	}
	public interface IEmitted
	{

	}
}



