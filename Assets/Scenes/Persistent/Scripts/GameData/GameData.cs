﻿
using System;

[Serializable]

public class GameData : GameDataFile
{
	public NoticeGameData[] m_noticeList;
	public RaceGameData[] m_raceList;
	public ShipGameData m_shipGameData;
	public EnginesGameData[] m_enginesList;
	public SheildingGameData[] m_shieldingList;
	public ArmorGameData[] m_armorList;
	public MissileLauncherGameData[] m_missileLauncherList;
	public LaserCannonGameData[] m_laserCannonList;
	public ArtifactGameData[] m_artifactList;
	public ElementGameData[] m_elementList;

	public int FindElementId( string name )
	{
		for ( int elementId = 0; elementId < m_elementList.Length; elementId++ )
		{
			if ( m_elementList[ elementId ].m_name == name )
			{
				return elementId;
			}
		}

		return -1;
	}
}
