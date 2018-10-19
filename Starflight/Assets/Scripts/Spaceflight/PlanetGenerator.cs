﻿
using System.IO;
using System.IO.Compression;
using UnityEngine;

public class PlanetGenerator
{
	// generator constants
	const int c_rockyPlanetTextureMapScaleX = 42;
	const int c_rockyPlanetTextureMapScaleY = 34;

	const int c_gasGiantTextureMapScaleX = 10;
	const int c_gasGiantTextureMapScaleY = 8;

	const int c_xBlurRadiusGasGiant = 255;
	const int c_yBlurRadiusGasGiant = 5;

	const float c_normalScale = 10.0f;

	// the planet
	Planet m_planet;

	// remember where we were in the maps generation process
	int m_currentStep;

	// resource request handle
	ResourceRequest m_resourceRequest;

	// the legend
	Color[] m_legend;

	// the elevation buffer
	float[,] m_elevationBuffer;

	// difference buffer
	byte[] m_differenceBuffer;

	// difference buffer parameters
	float m_minimumDifference;
	float m_maximumDifference;

	// texture map scale
	int m_textureMapScaleX;
	int m_textureMapScaleY;

	// the texture map size
	int m_textureWidth;
	int m_textureHeight;

	// the buffers
	Color[,] m_albedoBuffer;
	Color[,] m_effectsBuffer;
	Color[,] m_normalBuffer;

	// remember if we are done
	public bool m_mapsGenerated;

	// the texture maps
	public Texture2D m_albedoTexture;
	public Texture2D m_effectsTexture;
	public Texture2D m_normalTexture;
	public Texture2D m_legendTexture;

	public void Start( Planet planet )
	{
		m_planet = planet;
		m_mapsGenerated = false;
		m_currentStep = 0;
	}

	public float Process()
	{
		switch ( m_currentStep )
		{
			case 0:
				StartLoadingPlanetData();
				return 0.0f;

			case 1:
				WaitForPlanetData();
				return 0.1f;

			case 2:
				DecompressPlanetData();
				return 0.2f;

			case 3:
				DoContours();
				return 0.3f;

			case 4:
				DoScaleToPowerOfTwo();
				return 0.4f;

			case 5:
				FinalizeElevationBuffer();
				return 0.5f;

			case 6:
				CreateAlbedoBuffer();
				return 0.6f;

			case 7:
				CreateAlbedoTexture();
				return 0.7f;

			case 8:
				CreateEffectsBuffer();
				return 0.8f;

			case 9:
				CreateEffectsTexture();
				return 0.9f;

			case 10:
				CreateNormalBuffer();
				return 1.0f;

			case 11:
				CreateNormalTexture();
				return 1.1f;

			case 12:
				CreateLegendTexture();
				return 1.2f;

			default:

				return 0.0f;
		}
	}

	void StartLoadingPlanetData()
	{
		m_resourceRequest = Resources.LoadAsync( "Planets/" + m_planet.m_id, typeof( TextAsset ) );

		m_currentStep++;
	}

	void WaitForPlanetData()
	{
		if ( m_resourceRequest.isDone )
		{
			m_currentStep++;
		}
	}

	void DecompressPlanetData()
	{
		var compressedPlanetData = m_resourceRequest.asset as TextAsset;

		if ( compressedPlanetData == null )
		{
			m_elevationBuffer = PrepareMap();

			m_legend = new Color[ 1 ];

			m_legend[ 1 ] = new Color( 1.0f, 0.65f, 0.0f );
		}
		else
		{
			using ( var memoryStream = new MemoryStream( compressedPlanetData.bytes ) )
			{
				using ( var gZipStream = new GZipStream( memoryStream, CompressionMode.Decompress, false ) )
				{
					var binaryReader = new BinaryReader( gZipStream );

					var version = binaryReader.ReadInt32();

					if ( version != 1 )
					{
						m_elevationBuffer = PrepareMap();

						m_legend = new Color[ 1 ];

						m_legend[ 1 ] = new Color( 1.0f, 0.65f, 0.0f );
					}
					else
					{
						var legendLength = binaryReader.ReadInt32();

						m_legend = new Color[ legendLength ];

						for ( var i = 0; i < legendLength; i++ )
						{
							m_legend[ i ].r = binaryReader.ReadSingle();
							m_legend[ i ].g = binaryReader.ReadSingle();
							m_legend[ i ].b = binaryReader.ReadSingle();
							m_legend[ i ].a = binaryReader.ReadSingle();
						}

						var preparedMapWidth = binaryReader.ReadInt32();
						var preparedMapHeight = binaryReader.ReadInt32();

						m_elevationBuffer = new float[ preparedMapHeight, preparedMapWidth ];

						for ( var y = 0; y < preparedMapHeight; y++ )
						{
							for ( var x = 0; x < preparedMapWidth; x++ )
							{
								m_elevationBuffer[ y, x ] = binaryReader.ReadSingle();
							}
						}

						if ( !m_planet.IsGasGiant() )
						{
							m_minimumDifference = binaryReader.ReadSingle();
							m_maximumDifference = binaryReader.ReadSingle();

							var differenceBufferWidth = 2048;
							var differenceBufferHeight = 1024;
							var differenceBufferSize = differenceBufferWidth * differenceBufferHeight;

							m_differenceBuffer = new byte[ differenceBufferSize ];

							gZipStream.Read( m_differenceBuffer, 0, differenceBufferSize );
						}
					}
				}
			}
		}

		m_currentStep++;
	}

	void DoContours()
	{
		if ( m_planet.IsGasGiant() )
		{
			m_textureMapScaleX = c_gasGiantTextureMapScaleX;
			m_textureMapScaleY = c_gasGiantTextureMapScaleY;
		}
		else
		{
			m_textureMapScaleX = c_rockyPlanetTextureMapScaleX;
			m_textureMapScaleY = c_rockyPlanetTextureMapScaleY;
		}

		var contours = new Contours( m_elevationBuffer );

		m_elevationBuffer = contours.Process( m_textureMapScaleX, m_textureMapScaleY, m_legend );

		m_currentStep++;
	}

	void DoScaleToPowerOfTwo()
	{
		var scaleToPowerOfTwo = new ScaleToPowerOfTwo( m_elevationBuffer );

		m_elevationBuffer = scaleToPowerOfTwo.Process( m_textureMapScaleX, m_textureMapScaleY );

		m_currentStep++;
	}

	void FinalizeElevationBuffer()
	{
		m_textureWidth = m_elevationBuffer.GetLength( 1 );
		m_textureHeight = m_elevationBuffer.GetLength( 0 );

		if ( m_planet.IsGasGiant() )
		{
			var gaussianBlur = new GaussianBlur( m_elevationBuffer );

			m_elevationBuffer = gaussianBlur.Process( c_xBlurRadiusGasGiant, c_yBlurRadiusGasGiant );
		}
		else if ( m_differenceBuffer != null )
		{
			var elevationScale = ( m_maximumDifference - m_minimumDifference ) / 255.0f;

			for ( var y = 0; y < m_textureHeight; y++ )
			{
				for ( var x = 0; x < m_textureWidth; x++ )
				{
					var difference = m_differenceBuffer[ y * m_textureWidth + x ];

					m_elevationBuffer[ y, x ] += ( difference * elevationScale ) + m_minimumDifference;
				}
			}
		}

		m_currentStep++;
	}

	void CreateAlbedoBuffer()
	{
		var albedo = new Albedo( m_elevationBuffer );

		m_albedoBuffer = albedo.Process( m_legend );

		m_currentStep++;
	}

	void CreateAlbedoTexture()
	{
		var pixels = new Color[ m_textureWidth * m_textureHeight ];

		var index = 0;

		for ( var y = 0; y < m_textureHeight; y++ )
		{
			for ( var x = 0; x < m_textureWidth; x++ )
			{
				pixels[ index++ ] = m_albedoBuffer[ y, x ];
			}
		}

		m_albedoTexture = new Texture2D( m_textureWidth, m_textureHeight, TextureFormat.RGB24, true );

		m_albedoTexture.SetPixels( pixels );

		m_albedoTexture.filterMode = FilterMode.Trilinear;
		m_albedoTexture.wrapModeU = TextureWrapMode.Repeat;
		m_albedoTexture.wrapModeV = TextureWrapMode.Clamp;

		m_albedoTexture.Apply();

		m_albedoTexture.Compress( true );

		m_currentStep++;
	}

	void CreateEffectsBuffer()
	{
		m_effectsBuffer = new Color[ m_textureHeight, m_textureWidth ];

		for ( var y = 0; y < m_textureHeight; y++ )
		{
			for ( var x = 0; x < m_textureWidth; x++ )
			{
				// get the albedo color
				var color = m_albedoBuffer[ y, x ];

				var water = ( !m_planet.IsGasGiant() && ( color.a < 0.5f ) ) ? 1.0f : 0.0f;

				// make it shiny where water is
				var roughness = ( water == 1.0f ) ? 0.3f : 1.0f;

				// add in roughness due to snow on mountains (snow = add in sharp gloss)
				roughness = Mathf.Lerp( roughness, 0.3f, ( color.a - 2.0f ) * 0.5f );

				// calculate reflectivity based on roughness (sharp gloss = also reflective, dull gloss = not so reflective)
				var reflectivity = ( 1.0f - roughness ) * 0.5f;

				// put it all together
				m_effectsBuffer[ y, x ] = new Color( roughness, water, reflectivity );
			}
		}

		m_currentStep++;
	}

	void CreateEffectsTexture()
	{
		var pixels = new Color[ m_textureWidth * m_textureHeight ];

		var index = 0;

		for ( var y = 0; y < m_textureHeight; y++ )
		{
			for ( var x = 0; x < m_textureWidth; x++ )
			{
				pixels[ index++ ] = m_effectsBuffer[ y, x ];
			}
		}

		m_effectsTexture = new Texture2D( m_textureWidth, m_textureHeight, TextureFormat.RGB24, true );

		m_effectsTexture.SetPixels( pixels );

		m_effectsTexture.filterMode = FilterMode.Trilinear;
		m_effectsTexture.wrapModeU = TextureWrapMode.Repeat;
		m_effectsTexture.wrapModeV = TextureWrapMode.Clamp;

		m_effectsTexture.Apply();

		m_effectsTexture.Compress( true );

		m_currentStep++;
	}

	void CreateNormalBuffer()
	{
		var normals = new Normals( m_elevationBuffer );

		m_normalBuffer = normals.Process( m_planet.IsGasGiant() ? 1.0f : c_normalScale );

		m_currentStep++;
	}

	void CreateNormalTexture()
	{
		var pixels = new Color[ m_textureWidth * m_textureHeight ];

		var index = 0;

		for ( var y = 0; y < m_textureHeight; y++ )
		{
			for ( var x = 0; x < m_textureWidth; x++ )
			{
				pixels[ index++ ] = m_normalBuffer[ y, x ];
			}
		}

		m_normalTexture = new Texture2D( m_textureWidth, m_textureHeight, TextureFormat.RGBA32, true );

		m_normalTexture.SetPixels( pixels );

		m_normalTexture.filterMode = FilterMode.Trilinear;
		m_normalTexture.wrapModeU = TextureWrapMode.Repeat;
		m_normalTexture.wrapModeV = TextureWrapMode.Clamp;

		m_normalTexture.Apply();

		m_normalTexture.Compress( true );

		m_currentStep++;
	}

	void CreateLegendTexture()
	{
		m_legendTexture = new Texture2D( 1, m_legend.Length, TextureFormat.RGB24, false );

		for ( var i = 0; i < m_legend.Length; i++ )
		{
			m_legendTexture.SetPixel( 0, i, m_legend[ i ] );
		}

		m_legendTexture.filterMode = FilterMode.Bilinear;
		m_legendTexture.wrapMode = TextureWrapMode.Clamp;

		m_legendTexture.Apply();

		// all done!
		m_mapsGenerated = true;

		// free up memory
		m_legend = null;
		m_elevationBuffer = null;
		m_differenceBuffer = null;
		m_albedoBuffer = null;
		m_effectsBuffer = null;
		m_normalBuffer = null;
	}

	// generates the map for a planet we don't have data for
	float[,] PrepareMap()
	{
		var preparedMap = new float[ 1, 1 ];

		preparedMap[ 0, 0 ] = 0.0f;

		return preparedMap;
	}
}
