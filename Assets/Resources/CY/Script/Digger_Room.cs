using UnityEngine;
using System.Collections.Generic;

public class Digger_Room : Room
{
    public GameObject ratWallPrefab;
    public GameObject cheesePrefab;

    public override void fillRoom(LevelGenerator ourGenerator, ExitConstraint requiredExits)
    {
        bool[,] wallMap = new bool[LevelGenerator.ROOM_WIDTH, LevelGenerator.ROOM_HEIGHT];

        for (int x = 0; x < LevelGenerator.ROOM_WIDTH; x++)
        {
            for (int y = 0; y < LevelGenerator.ROOM_HEIGHT; y++)
            {
                wallMap[x, y] = true;
            }
        }

        //dig the exit
        //wallMap[LevelGenerator.ROOM_WIDTH / 2, LevelGenerator.ROOM_HEIGHT - 1] = false;
        //wallMap[LevelGenerator.ROOM_WIDTH - 1, LevelGenerator.ROOM_HEIGHT / 2] = false;
        //wallMap[LevelGenerator.ROOM_WIDTH / 2, 0] = false;
        //wallMap[0, LevelGenerator.ROOM_HEIGHT / 2] = false;

        List<Vector2Int>  edgePoints = new List<Vector2Int>();
        edgePoints.Add(new Vector2Int(LevelGenerator.ROOM_WIDTH / 2, LevelGenerator.ROOM_HEIGHT - 1));
        edgePoints.Add(new Vector2Int(LevelGenerator.ROOM_WIDTH - 1, LevelGenerator.ROOM_HEIGHT / 2));
        edgePoints.Add(new Vector2Int(LevelGenerator.ROOM_WIDTH / 2, 0));
        edgePoints.Add(new Vector2Int(0, LevelGenerator.ROOM_HEIGHT / 2));

        //random spwan a digger at the edge points
        int startPoint = Random.Range(0, edgePoints.Count);
        int diggerX = edgePoints[startPoint].x;
        int diggerY = edgePoints[startPoint].y;
        wallMap[diggerX, diggerY] = false;

        //dicide the direction of the digger
        int xDirection = 0;
        int yDirection = 0;
        if(diggerY == LevelGenerator.ROOM_HEIGHT - 1)
        {
            yDirection = -1;
        }
        else if(diggerY == 0)
        {
            yDirection = 1;
        }
        else if(diggerX == LevelGenerator.ROOM_WIDTH - 1)
        {
            xDirection = -1;
        }
        else if(diggerX == 0)
        {
            xDirection = 1;
        }


        float turnProbability = 0.5f;
        float makeARatProbability = 0.5f;
        float makeACheeseProbability = 0.5f;
        //start to dig
        while(true)
        {
            wallMap[diggerX, diggerY] = false;

            if(Random.Range(0f, 1f) < turnProbability)
            {
                if(Random.Range(0f, 1f) < 0.5f)
                {
                    int tempX = xDirection;
                    int tempY = yDirection;
                    
                    xDirection = tempY;
                    yDirection = tempX;
                }
                else
                {
                    int tempX = xDirection;
                    int tempY = yDirection;
                    
                    xDirection = -tempY;
                    yDirection = -tempX;
                }
                if(Random.Range(0f, 1f) < makeARatProbability)
                {
                    if(Random.Range(0f, 1f) < makeACheeseProbability)
                    {
                        Tile.spawnTile(cheesePrefab, transform, diggerX, diggerY);
                    }
                    else
                    {
                        Tile.spawnTile(ratWallPrefab, transform, diggerX, diggerY);
                    }
                }
                diggerX += xDirection;
                diggerY += yDirection;
            }
            else
            {
                diggerX += xDirection;
                diggerY += yDirection;
            }

            if(diggerX < 0 || diggerX >= LevelGenerator.ROOM_WIDTH || diggerY < 0 || diggerY >= LevelGenerator.ROOM_HEIGHT)
            {
                break;
            }

            
            
        }

        
        
    



        //spawn the walls
        for (int x = 0; x < LevelGenerator.ROOM_WIDTH; x++)
        {
            for (int y = 0; y < LevelGenerator.ROOM_HEIGHT; y++)
            {
                if (wallMap[x, y])
                {
                    Tile.spawnTile(ourGenerator.normalWallPrefab, transform, x, y);
                }
            }
        }
    }


    

}
