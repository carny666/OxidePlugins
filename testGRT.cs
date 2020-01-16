using Facepunch;
using Oxide.Core;
using Oxide.Core.Libraries;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

// TODO: Add chat commands to change new metabolism limits

namespace Oxide.Plugins
{
    [Info("testGRT", "carny666", "1.0.12")]
    class testGRT : RustPlugin
    {
        const float calgon = 0.00675f;
        List<SpawnPosition> spawnGrid = new List<SpawnPosition>();


        void Init()
        {
            spawnGrid = CreateSpawnGrid();
        }


        List<SpawnPosition> CreateSpawnGrid()
        {
            try
            {
                List<SpawnPosition> retval = new List<SpawnPosition>();

                var worldSize = (ConVar.Server.worldsize);
                float offset = worldSize / 2;
                var gridWidth = (calgon * worldSize);
                float step = worldSize / gridWidth;
                string start = "";

                char letter = 'A';
                int number = 0;

                for (float zz = offset; zz > -offset; zz -= step)
                {
                    for (float xx = -offset; xx < offset; xx += step)
                    {
                        var sp = new SpawnPosition(new Vector3(xx, 0, zz));
                        sp.GridReference = $"{start}{letter}{number}";
                        retval.Add(sp);
                        if (letter.ToString().ToUpper() == "Z")
                        {
                            start = "A";
                            letter = 'A';
                        }
                        else
                        {
                            letter = (char)(((int)letter) + 1);
                        }


                    }
                    number++;
                    start = "";
                    letter = 'A';
                }
                return retval;
            }
            catch (Exception ex)
            {
                throw new Exception($"CreateSpawnGrid {ex.Message}");
            }
        }


    }
    class SpawnPosition
    {
        const float aboveGoundPosition = 2.5f;

        public Vector3 Position;
        public Vector3 GroundPosition;
        public string GridReference;

        public SpawnPosition(Vector3 position)
        {
            Position = position;
            GroundPosition = GetGroundPosition(new Vector3(position.x, 25, position.z));
        }

        public bool isPositionAboveWater()
        {
            if ((TerrainMeta.HeightMap.GetHeight(Position) - TerrainMeta.WaterMap.GetHeight(Position)) >= 0)
                return false;
            return true;
        }

        public float WaterDepthAtPosition()
        {
            return (TerrainMeta.WaterMap.GetHeight(Position) - TerrainMeta.HeightMap.GetHeight(Position));
        }

        Vector3 GetGroundPosition(Vector3 sourcePos)
        {

            RaycastHit hitInfo;
            if (Physics.Raycast(sourcePos, Vector3.down, out hitInfo))
                sourcePos.y = hitInfo.point.y;

            sourcePos.y = Mathf.Max(sourcePos.y, TerrainMeta.HeightMap.GetHeight(sourcePos)) + aboveGoundPosition;

            return sourcePos;
        }

    }

}
