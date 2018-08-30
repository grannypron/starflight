﻿
public class ScienceButton : ShipButton
{
	private readonly ShipButton[] m_buttons = { new SensorsButton(), new AnalysisButton(), new StatusButton(), new BridgeButton() };

	public override string GetLabel()
	{
		return "Science";
	}

	public override bool Execute()
	{
		// change the buttons
		m_spaceflightController.m_buttonController.UpdateButtons( m_buttons );

		// get to the player data
		PlayerData playerData = DataController.m_instance.m_playerData;

		// get the personnel file on this officer
		Personnel.PersonnelFile personnelFile = playerData.m_crewAssignment.GetPersonnelFile( CrewAssignment.Role.ScienceOfficer );

		// set the name of the officer
		m_spaceflightController.m_spaceflightUI.ChangeOfficerText( "Officer " + personnelFile.m_name );

		return true;
	}

	public override void Cancel()
	{
		// play the deactivate sound
		SoundController.m_instance.PlaySound( SoundController.Sound.Deactivate );

		// return to the bridge
		m_spaceflightController.m_buttonController.RestoreBridgeButtons();
	}
}
