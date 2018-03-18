using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;

public class JobPract : MonoBehaviour
{
	
	NativeArray<int>	nas;

	private void Awake()
	{
		nas = new NativeArray<int>( 1000, Allocator.Persistent, NativeArrayOptions.UninitializedMemory );

	}

	private void OnDestroy()
	{
		nas.Dispose();
	}


}
