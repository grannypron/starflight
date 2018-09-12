﻿
using System;

[Serializable]

public class GameData : GameDataFile
{
	public Misc m_misc;
	public Starport m_starport;
	public Notice[] m_noticeList;
	public Race[] m_raceList;
	public Ship m_ship;
	public Engines[] m_enginesList;
	public Sheilding[] m_shieldingList;
	public Armor[] m_armorList;
	public MissileLauncher[] m_missileLauncherList;
	public LaserCannon[] m_laserCannonList;
	public Artifact[] m_artifactList;
	public Element[] m_elementList;
	public Star[] m_starList;
	public Planet[] m_planetList;
	public PlanetType[] m_planetTypeList;
	public Atmosphere[] m_atmosphereList;
	public AtmosphereDensity[] m_atmosphereDensityList;
	public Hydrosphere[] m_hydrosphereList;
	public Surface[] m_surfaceList;
	public Temperature[] m_temperatureList;
	public Weather[] m_weatherList;
	public Flux[] m_fluxList;
	public Territory[] m_territoryList;
	public Nebula[] m_nebulaList;
	public SpectralClass[] m_spectralClassList;

	public void Initialize()
	{
		// go through each planet
		foreach ( Planet planet in m_planetList )
		{
			// initialize it
			planet.Initialize();
		}

		// go through each star
		foreach ( Star star in m_starList )
		{
			// initialize it
			star.Initialize( this );
		}

		// go through each flux
		foreach ( Flux flux in m_fluxList )
		{
			// initialize it
			flux.Initialize();
		}

		// go through each territory
		foreach ( Territory territory in m_territoryList )
		{
			// initialize it
			territory.Initialize();
		}

		// go through each nebula
		foreach ( Nebula nebula in m_nebulaList )
		{
			// initialize it
			nebula.Initialize();
		}
	}

	// this finds the element in the list by its name
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
