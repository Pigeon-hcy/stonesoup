using System.Collections.Generic;
using UnityEngine;

public class EnemyAndGoodThingRoom : Room
{
	public GameObject treasurePrefab;
	public GameObject enemy1Prefab;
	public GameObject enemy2Prefab;
	public GameObject enemy3Prefab;
	public float borderWallProbability = 0.7f;

	public int minEnemies = 4, maxEnemies = 14;

	public override void fillRoom(LevelGenerator ourGenerator, ExitConstraint requiredExits)
	{
		// 1. 边上随机生成墙壁（出口位置留空）
		generateWalls(ourGenerator, requiredExits);

		// 2. 占用数组：边界视为已占用
		bool[,] occupiedPositions = new bool[LevelGenerator.ROOM_WIDTH, LevelGenerator.ROOM_HEIGHT];
		for (int x = 0; x < LevelGenerator.ROOM_WIDTH; x++)
		{
			for (int y = 0; y < LevelGenerator.ROOM_HEIGHT; y++)
			{
				occupiedPositions[x, y] = (x == 0 || x == LevelGenerator.ROOM_WIDTH - 1
					|| y == 0 || y == LevelGenerator.ROOM_HEIGHT - 1);
			}
		}

		// 3. 房间正中间放宝藏
		int centerX = LevelGenerator.ROOM_WIDTH / 2;
		int centerY = LevelGenerator.ROOM_HEIGHT / 2;
		Tile.spawnTile(treasurePrefab, transform, centerX, centerY);
		occupiedPositions[centerX, centerY] = true;

		// 4. 在其余空位里随机放敌人（数量在 minEnemies～maxEnemies，类型在 enemy1/2/3 中随机）
		List<GameObject> enemyPrefabs = new List<GameObject> { enemy1Prefab, enemy2Prefab, enemy3Prefab };
		List<Vector2> possibleSpawnPositions = new List<Vector2>(LevelGenerator.ROOM_WIDTH * LevelGenerator.ROOM_HEIGHT);
		int numEnemies = Random.Range(minEnemies, maxEnemies + 1);

		for (int i = 0; i < numEnemies; i++)
		{
			possibleSpawnPositions.Clear();
			for (int x = 0; x < LevelGenerator.ROOM_WIDTH; x++)
			{
				for (int y = 0; y < LevelGenerator.ROOM_HEIGHT; y++)
				{
					if (!occupiedPositions[x, y])
						possibleSpawnPositions.Add(new Vector2(x, y));
				}
			}
			if (possibleSpawnPositions.Count > 0)
			{
				Vector2 spawnPos = GlobalFuncs.randElem(possibleSpawnPositions);
				GameObject enemyPrefab = GlobalFuncs.randElem(enemyPrefabs);
				Tile.spawnTile(enemyPrefab, transform, (int)spawnPos.x, (int)spawnPos.y);
				occupiedPositions[(int)spawnPos.x, (int)spawnPos.y] = true;
			}
		}
	}

	protected void generateWalls(LevelGenerator ourGenerator, ExitConstraint requiredExits)
	{
		bool[,] wallMap = new bool[LevelGenerator.ROOM_WIDTH, LevelGenerator.ROOM_HEIGHT];
		for (int x = 0; x < LevelGenerator.ROOM_WIDTH; x++)
		{
			for (int y = 0; y < LevelGenerator.ROOM_HEIGHT; y++)
			{
				if (x == 0 || x == LevelGenerator.ROOM_WIDTH - 1
					|| y == 0 || y == LevelGenerator.ROOM_HEIGHT - 1)
				{
					// 出口位置不造墙
					if (x == LevelGenerator.ROOM_WIDTH / 2 && y == LevelGenerator.ROOM_HEIGHT - 1 && requiredExits.upExitRequired)
						wallMap[x, y] = false;
					else if (x == LevelGenerator.ROOM_WIDTH - 1 && y == LevelGenerator.ROOM_HEIGHT / 2 && requiredExits.rightExitRequired)
						wallMap[x, y] = false;
					else if (x == LevelGenerator.ROOM_WIDTH / 2 && y == 0 && requiredExits.downExitRequired)
						wallMap[x, y] = false;
					else if (x == 0 && y == LevelGenerator.ROOM_HEIGHT / 2 && requiredExits.leftExitRequired)
						wallMap[x, y] = false;
					else
						wallMap[x, y] = Random.value <= borderWallProbability;
				}
				else
				{
					wallMap[x, y] = false;
				}
			}
		}

		for (int x = 0; x < LevelGenerator.ROOM_WIDTH; x++)
		{
			for (int y = 0; y < LevelGenerator.ROOM_HEIGHT; y++)
			{
				if (wallMap[x, y])
					Tile.spawnTile(ourGenerator.normalWallPrefab, transform, x, y);
			}
		}
	}
}
