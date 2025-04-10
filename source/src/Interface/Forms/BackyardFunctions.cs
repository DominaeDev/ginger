using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Ginger.Properties;
using Ginger.Integration;

using WinAPICodePack = Microsoft.WindowsAPICodePack.Dialogs;

namespace Ginger
{
	using CharacterInstance = Backyard.CharacterInstance;
	using GroupInstance = Backyard.GroupInstance;
	using FolderInstance = Backyard.FolderInstance;
	using ChatInstance = Backyard.ChatInstance;
	using ImageInstance = Backyard.ImageInstance;

	public partial class MainForm
	{
		private bool ConnectToBackyard()
		{
			var error = Backyard.EstablishConnection();
			if (error == Backyard.Error.ValidationFailed)
			{
				MsgBox.Error(Resources.error_link_unsupported, Resources.cap_link_connect);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}
			else if (error == Backyard.Error.NotConnected)
			{
				MsgBox.Error(Resources.error_link_not_installed, Resources.cap_link_connect);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(string.Format(Resources.error_link_failed_with_reason, Backyard.LastError ?? ""), Resources.cap_link_error);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}
			else
			{
				// Fetch characters
				if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
				{
					// Error
					MsgBox.LinkError.RefreshFailed(Resources.cap_link_connect);
					AppSettings.BackyardLink.Enabled = false;
					return false;
				}
				else
				{
					if (Current.HasLink)
						Current.Link.RefreshState();

					MsgBox.Message(Resources.msg_link_connected, Resources.cap_link_connect);
					SetStatusBarMessage(Resources.status_link_connect, Constants.StatusBarMessageInterval);
					AppSettings.BackyardLink.Enabled = true;

					VersionNumber appVersion;
					if (Backyard.GetAppVersion(out appVersion))
						AppSettings.BackyardLink.LastVersion = appVersion;
					return true;
				}
			}
		}

		private bool DisconnectFromBackyard()
		{
			SetStatusBarMessage(Resources.status_link_disconnect, Constants.StatusBarMessageInterval);
			AppSettings.BackyardLink.Enabled = false;
			Backyard.Disconnect();
			RefreshTitle();
			return true;
		}
		
		private Backyard.Error RunTask(Func<Backyard.Error> action, string statusText = null)
		{
			if (statusText != null)
				SetStatusBarMessage(statusText);
			
			this.Cursor = Cursors.WaitCursor;
			var error = action.Invoke();
			this.Cursor = Cursors.Default;

			if (statusText != null)
				ClearStatusBarMessage();
			return error;
		}
		
		private string NumCharacters(int n)
		{
			return n == 1 ? string.Concat(n.ToString(), " character") : string.Concat(n.ToString(), " characters");
		}

		private string NumGroups(int n)
		{
			return n == 1 ? string.Concat(n.ToString(), " chat") : string.Concat(n.ToString(), " chats");
		}

		private bool ImportCharacterFromBackyard()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_import_character);
				AppSettings.BackyardLink.Enabled = false;
			}

			var dlg = new LinkSelectCharacterOrGroupDialog();
			dlg.Options = LinkSelectCharacterOrGroupDialog.Option.Solo;
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
				dlg.Options |= LinkSelectCharacterOrGroupDialog.Option.Parties | LinkSelectCharacterOrGroupDialog.Option.Orphans;
			dlg.ConfirmButton = "Open";
			dlg.Text = "Open Backyard AI character";
			if (dlg.ShowDialog() != DialogResult.OK)
				return false;

			if (dlg.SelectedGroup.isParty)
				return ImportGroupFromBackyard(dlg.SelectedGroup);

			if (ConfirmSave(Resources.cap_import_character) == false)
				return false;

			SetStatusBarMessage(Resources.status_open_character);

			// Import...
			FaradayCardV4 faradayData;
			ImageInstance[] images;
			UserData userInfo;
			CharacterInstance characterInstance = dlg.SelectedCharacter;
			var importError = Backyard.Database.ImportCharacter(characterInstance.instanceId, out faradayData, out images, out userInfo);
			if (importError == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_open_character, Resources.cap_import_character);
				ClearStatusBarMessage();
				return false;
			}
			else if (importError != Backyard.Error.NoError || faradayData == null)
			{
				MsgBox.Error(Resources.error_link_open_character, Resources.cap_import_character);
				ClearStatusBarMessage();
				return false;
			}

			if (AppSettings.BackyardLink.WriteUserPersona == false)
			{
				images = images.Except(i => i.imageType == AssetFile.AssetType.UserIcon);
				userInfo = null;
			}

			// Success
			Current.ReadFaradayCard(faradayData, null, userInfo);

			Backyard.Link.Image[] imageLinks;
			Current.ImportImages(images, null, out imageLinks);

			ClearStatusBarMessage();

			FileMutex.Release();

			Current.Filename = null;
			Current.IsDirty = false;
			Current.IsFileDirty = false;
			Current.OnLoadCharacter?.Invoke(this, EventArgs.Empty);

			if (AppSettings.BackyardLink.AlwaysLinkOnImport 
				|| MsgBox.Ask(Resources.msg_link_create_link, Resources.cap_link_character))
			{
				Current.LinkWith(characterInstance, imageLinks);
				SetStatusBarMessage(Resources.status_link_create, Constants.StatusBarMessageInterval);
				Current.IsFileDirty = false;
				Current.IsLinkDirty = false;
				RefreshTitle();
			}
			return true;
		}

		private bool ImportGroupFromBackyard(GroupInstance groupInstance)
		{
			if (ConfirmSave(Resources.cap_import_character) == false)
				return false;

			SetStatusBarMessage(Resources.status_open_character);

			FaradayCardV4[] faradayData;
			CharacterInstance[] characterInstances;
			ImageInstance[] images;
			UserData userInfo;
			var importError = Backyard.Database.ImportParty(groupInstance.instanceId, out faradayData, out characterInstances, out images, out userInfo);
			if (importError == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_open_character, Resources.cap_import_character);
				ClearStatusBarMessage();
				return false;
			}
			else if (importError != Backyard.Error.NoError || faradayData == null || faradayData.Length == 0)
			{
				MsgBox.Error(Resources.error_link_open_character, Resources.cap_import_character);
				ClearStatusBarMessage();
				return false;
			}

			if (AppSettings.BackyardLink.WriteUserPersona == false)
			{
				images = images.Except(i => i.imageType == AssetFile.AssetType.UserIcon);
				userInfo = null;
			}

			// Success
			Current.ReadFaradayCards(faradayData, null, userInfo);

			Backyard.Link.Image[] imageLinks;
			int[] actorIndices = new int[images.Length];

			for (int i = 0; i < images.Length; ++i)
			{
				string instanceId = images[i].associatedInstanceId;
				actorIndices[i] = Array.FindIndex(characterInstances, c => c.instanceId == instanceId);
			}

			Current.ImportImages(images, actorIndices, out imageLinks);

			// Resolve actor indices for imported assets
			foreach (var asset in Current.Card.assets.Where(a => a.assetType == AssetFile.AssetType.Icon))
			{
				var assetUID = asset.uid;
				int idxLink = Array.FindIndex(imageLinks, l => l.uid == assetUID);
				if (idxLink >= 0 && idxLink < images.Length)
				{
					string instanceId = images[idxLink].associatedInstanceId;
					int idxActor = Array.FindIndex(characterInstances, c => c.instanceId == instanceId);
					if (idxActor != -1)
						asset.actorIndex = idxActor;
				}
			}

			ClearStatusBarMessage();

			FileMutex.Release();

			Current.Filename = null;
			Current.IsDirty = false;
			Current.IsFileDirty = false;
			Current.OnLoadCharacter?.Invoke(this, EventArgs.Empty);

			if (AppSettings.BackyardLink.AlwaysLinkOnImport 
				|| MsgBox.Ask(Resources.msg_link_create_link, Resources.cap_link_character))
			{
				Current.LinkWith(groupInstance, characterInstances, imageLinks);
				SetStatusBarMessage(Resources.status_link_create, Constants.StatusBarMessageInterval);
				Current.IsFileDirty = false;
				Current.IsLinkDirty = false;
				RefreshTitle();
			}
			return true;
		}

		private bool SaveCharacterAsNewToBackyard()
		{
			CharacterInstance createdCharacter;
			Backyard.Link.Image[] images;

			var error = CreateNewCharacterInBackyard(out createdCharacter, out images);
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.ConnectionFailed();
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_save_character_as_new, Resources.cap_link_save_character);
				return false;
			}
			else
			{
				if (AppSettings.BackyardLink.AlwaysLinkOnImport 
					|| MsgBox.Ask(Resources.msg_link_create_link, Resources.cap_link_character))
				{
					Current.LinkWith(createdCharacter, images);
					Current.IsLinkDirty = false;
					SetStatusBarMessage(Resources.status_link_save_and_link_new, Constants.StatusBarMessageInterval);
					RefreshTitle();
					MsgBox.Message(Resources.msg_link_save_and_link_new, Resources.cap_link_save_character);
				}
				else
				{
					MsgBox.Message(Resources.msg_link_saved, Resources.cap_link_save_character);
				}

				_bShouldRefreshSidePanel = true;
				return true;
			}
		}

		private Backyard.Error CreateNewCharacterInBackyard(out CharacterInstance createdCharacter, out Backyard.Link.Image[] images)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				createdCharacter = default(CharacterInstance);
				images = null;
				return Backyard.Error.NotConnected;
			}

			var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked);
			
			// User persona
			UserData userInfo = null;
			if (AppSettings.BackyardLink.WriteUserPersona)
			{
				string userPersona = output.userPersona.ToFaraday();
				if (string.IsNullOrEmpty(userPersona) == false)
				{
					userInfo = new UserData() {
						name = Current.Card.userPlaceholder,
						persona = userPersona,
					};
					output.userPersona = GingerString.Empty;
				}
			}

			FaradayCardV4 card = FaradayCardV4.FromOutput(output);
			card.EnsureSystemPrompt();

			Backyard.ImageInput[] imageInput = BackyardUtil.GatherImages();
			BackupData.Chat[] chats = null;
			if (AppSettings.BackyardLink.ImportAlternateGreetings && output.greetings.Length > 1)
				chats = BackupUtil.SplitAltGreetings(card, output, imageInput);

			var args = new Backyard.CreateCharacterArguments() {
				card = card,
				imageInput = imageInput,
				chats = chats,
				userInfo = userInfo,
			};

			var error = Backyard.Database.CreateNewCharacter(args, out createdCharacter, out images);
			if (error != Backyard.Error.NoError)
			{
				return error;
			}
			else
			{
				Current.IsFileDirty = true;
				Current.IsLinkDirty = false;
				RefreshTitle();

				// Refresh character information
				Backyard.RefreshCharacters();
				return Backyard.Error.NoError;
			}
		}
		
		private bool SaveCharacterChangesToBackyard()
		{
			Backyard.Error error;
			if (Current.HasLink == false)
				return false;
			
			if (Current.Link.linkType == Backyard.Link.LinkType.Group)
				error = UpdateGroupInBackyard();
			else
				error = UpdateCharacterInBackyard();

			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.ConnectionFailed();
				return false;
			}
			else if (error == Backyard.Error.NotFound)
			{
				var mr = MsgBox.AskYesNoCancel(Resources.error_link_character_not_found, Resources.cap_link_save_character);
				if (mr == DialogResult.Yes)
					return SaveCharacterAsNewToBackyard();

				if (mr == DialogResult.No)
					Current.BreakLink();
				return false;
			}
			else if (error == Backyard.Error.CancelledByUser || error == Backyard.Error.DismissedByUser)
			{
				// User clicked cancel
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_update_character, Resources.cap_link_save_character);
				return false;
			}
			else
			{
				SetStatusBarMessage(Resources.status_link_saved, Constants.StatusBarMessageInterval);
				//MsgBox.Message(Resources.msg_link_saved, Resources.cap_link_save_character);
				return true;
			}
		}

		private Backyard.Error UpdateCharacterInBackyard()
		{
			if (Backyard.ConnectionEstablished == false)
				return Backyard.Error.NotConnected;
			else if (Current.HasLink == false)
				return Backyard.Error.NotFound;

			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_overwrite);
				AppSettings.BackyardLink.Enabled = false;
			}

			// Check if character exists, has newer changes
			bool hasChanges;
			var error = Backyard.Database.ConfirmSaveCharacter(Current.Link, out hasChanges);
			if (error != Backyard.Error.NoError)
				return error;

			var output = Generator.Generate(Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked);

			// User persona
			UserData userInfo = null;
			if (AppSettings.BackyardLink.WriteUserPersona)
			{
				string userPersona = output.userPersona.ToFaraday();
				if (string.IsNullOrEmpty(userPersona) == false)
				{
					userInfo = new UserData() {
						name = Current.Card.userPlaceholder,
						persona = userPersona,
					};
					output.userPersona = GingerString.Empty;
				}
			}

			FaradayCardV4 card = FaradayCardV4.FromOutput(output);
			if (Current.Link.linkType != Backyard.Link.LinkType.StandAlone)
				card.EnsureSystemPrompt();

			if (hasChanges)
			{
				// Overwrite prompt
				var mr = MsgBox.ConfirmYesNoCancel(Resources.msg_link_confirm_overwrite, Resources.cap_link_overwrite);
				if (mr == DialogResult.Cancel)
					return Backyard.Error.CancelledByUser;
				else if (mr == DialogResult.No)
					return Backyard.Error.DismissedByUser;
			}

			DateTime updateDate;
			Backyard.Link.Image[] imageLinks;
			error = Backyard.Database.UpdateCharacter(Current.Link, card, userInfo, out updateDate, out imageLinks);
			if (error != Backyard.Error.NoError)
			{
				return error;
			}
			else
			{
				Current.Link.updateDate = updateDate;
				Current.Link.imageLinks = imageLinks;
				Current.IsFileDirty = true;
				Current.IsLinkDirty = false;
				RefreshTitle();

				// Refresh character information
				Backyard.RefreshCharacters();
				return Backyard.Error.NoError;
			}
		}

		private bool ReestablishLink()
		{
			// Refresh character information
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.ConnectionFailed();
				return false;
			}

			if (Current.Link != null)
			{
				bool bOk = false;
				Backyard.GroupInstance group;
				if (Current.Link.groupId != null && Backyard.Database.GetGroup(Current.Link.groupId, out group))
				{
					bOk = true;
				}
				else 
				{
					CharacterInstance characterInstance;
					if (Backyard.Database.GetCharacter(Current.Link.mainActorId, out characterInstance))
					{
						Current.Link.groupId = characterInstance.groupId;
						bOk = true;
					}
				}
				if (bOk)
				{
					Current.Link.filename = Current.Filename;
					Current.Link.isActive = true;
					Current.IsFileDirty = true;
					Current.Link.RefreshState();
					RefreshTitle();

					// MsgBox.Message(Resources.msg_link_reestablished, Resources.cap_link_reestablish);
					SetStatusBarMessage(Resources.status_link_reestablished, Constants.StatusBarMessageInterval);
					return true;
				}
				else
				{
					MsgBox.Error(Resources.error_link_reestablish, Resources.cap_link_reestablish);
					Current.Unlink();
					RefreshTitle();
				}
			}
			return false;
		}

		private bool BreakLink(bool bSilent = false)
		{
			if (Current.BreakLink())
			{
				RefreshTitle();

				if (bSilent == false)
					SetStatusBarMessage(Resources.status_link_break, Constants.StatusBarMessageInterval);
				return true;
			}
			return false;
		}

		private bool ReimportCharacterFromBackyard()
		{
			if (MsgBox.Confirm(Resources.msg_link_revert, Resources.cap_link_revert) == false)
				return false;

			var error = _RevertCharacterFromBackyard();
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.ConnectionFailed();
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_open_character, Resources.cap_link_revert);
				return false;
			}
			else
			{
				Current.IsLinkDirty = false;
				SetStatusBarMessage(Resources.status_link_reverted, Constants.StatusBarMessageInterval);
				RefreshTitle();
				return true;
			}
		}

		private Backyard.Error _RevertCharacterFromBackyard()
		{
			if (Backyard.ConnectionEstablished == false)
				return Backyard.Error.NotConnected;
			else if (Current.HasLink == false)
				return Backyard.Error.NotFound;

			// Refresh character list
			var refreshError = Backyard.RefreshCharacters();
			if (refreshError != Backyard.Error.NoError)
				return refreshError;

			// Get character instance
			CharacterInstance characterInstance;
			if (Backyard.Database.GetCharacter(Current.Link.mainActorId, out characterInstance) == false)
			{
				Current.BreakLink();
				return Backyard.Error.NotFound;
			}

			// Revert party?
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats) && Current.Link.groupId != null)
			{
				var group = Backyard.Database.GetGroup(Current.Link.groupId);
				if (group.isDefined)
					return RevertGroupFromBackyard(group);
			}

			SetStatusBarMessage(Resources.status_revert_character);

			// Import data
			FaradayCardV4 faradayData;
			ImageInstance[] images;
			UserData userInfo;
			var importError = Backyard.Database.ImportCharacter(characterInstance.instanceId, out faradayData, out images, out userInfo);
			if (importError != Backyard.Error.NoError)
				return importError;

			if (AppSettings.BackyardLink.WriteUserPersona == false)
			{
				images = images.Except(i => i.imageType == AssetFile.AssetType.UserIcon);
				userInfo = null;
			}

			// Success
			Current.ReadFaradayCard(faradayData, null, userInfo);

			Backyard.Link.Image[] imageLinks;
			Current.ImportImages(images, null, out imageLinks);

			Current.LinkWith(characterInstance, imageLinks);
			Current.IsDirty = true;
			Current.IsLinkDirty = false;
			
			RefreshAll();

			Undo.Push(Undo.Kind.RecipeList, "Reimport character");
			
			return Backyard.Error.NoError;
		}

		private Backyard.Error RevertGroupFromBackyard(GroupInstance groupInstance)
		{
			SetStatusBarMessage(Resources.status_revert_character);

			FaradayCardV4[] faradayData;
			CharacterInstance[] characterInstances;
			ImageInstance[] images;
			UserData userInfo;
			var importError = Backyard.Database.ImportParty(groupInstance.instanceId, out faradayData, out characterInstances, out images, out userInfo);
			if (faradayData == null || faradayData.Length == 0)
			{
				ClearStatusBarMessage();
				return Backyard.Error.NotFound;
			}
			else if (importError != Backyard.Error.NoError)
			{
				ClearStatusBarMessage();
				return importError;
			}

			if (AppSettings.BackyardLink.WriteUserPersona == false)
			{
				images = images.Except(i => i.imageType == AssetFile.AssetType.UserIcon);
				userInfo = null;
			}

			// Success
			Current.ReadFaradayCards(faradayData, null, userInfo);

			Backyard.Link.Image[] imageLinks;
			int[] actorIndices = new int[images.Length];

			for (int i = 0; i < images.Length; ++i)
			{
				string instanceId = images[i].associatedInstanceId;
				actorIndices[i] = Array.FindIndex(characterInstances, c => c.instanceId == instanceId);
			}

			Current.ImportImages(images, actorIndices, out imageLinks);

			// Resolve actor indices for imported assets
			foreach (var asset in Current.Card.assets.Where(a => a.assetType == AssetFile.AssetType.Icon))
			{
				var assetUID = asset.uid;
				int idxLink = Array.FindIndex(imageLinks, l => l.uid == assetUID);
				if (idxLink >= 0 && idxLink < images.Length)
				{
					string instanceId = images[idxLink].associatedInstanceId;
					int idxActor = Array.FindIndex(characterInstances, c => c.instanceId == instanceId);
					if (idxActor != -1)
						asset.actorIndex = idxActor;
				}
			}

			ClearStatusBarMessage();

			Current.LinkWith(groupInstance, characterInstances, imageLinks);
			SetStatusBarMessage(Resources.status_link_create, Constants.StatusBarMessageInterval);
			Current.IsFileDirty = false;
			Current.IsLinkDirty = false;
			
			RefreshAll();

			Undo.Push(Undo.Kind.RecipeList, "Revert character");
			
			return Backyard.Error.NoError;
		}

		private bool OpenChatHistory()
		{
			if (_editChatDialog != null && !_editChatDialog.IsDisposed)
				_editChatDialog.Close(); // Close existing

			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.ConnectionFailed();
				return false;
			}

			// Refresh character list
			var refreshError = Backyard.RefreshCharacters();
			if (refreshError != Backyard.Error.NoError)
				return false;

			_editChatDialog = new LinkEditChatDialog();
			if (Current.HasActiveLink)
			{
				GroupInstance group;
				if (Current.Link.groupId != null)
					group = Backyard.Database.GetGroup(Current.Link.groupId);
				else
					group = Backyard.Database.GetGroup(Backyard.Database.GetCharacter(Current.Link.mainActorId).groupId);

				if (group.isDefined)
					_editChatDialog.Group = group;
			}

			_editChatDialog.Show();
			return true;
		}

		public bool ExportManyCharactersFromBackyard()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_overwrite_files);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleCharactersOrGroupsDialog();
			dlg.Options = LinkSelectMultipleCharactersOrGroupsDialog.Option.Solo;
			dlg.Text = "Select characters to export";
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
				dlg.Options |= LinkSelectMultipleCharactersOrGroupsDialog.Option.Orphans;

			if (dlg.ShowDialog() != DialogResult.OK || dlg.SelectedCharacters.Length == 0)
				return false;

			CharacterInstance[] characterInstances = dlg.SelectedCharacters;

			// Export format
			var formatDialog = new FileFormatDialog();
			if (formatDialog.ShowDialog() != DialogResult.OK)
				return false;

			string ext;
			if (formatDialog.FileFormat.Contains(FileUtil.FileType.Json))
				ext = "json";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Png))
				ext = "png";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.CharX))
				ext = "charx";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Yaml))
				ext = "yaml";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Backup))
				ext = "zip";
			else
				return false; // Error

			var folderDialog = new WinAPICodePack.CommonOpenFileDialog();
			folderDialog.Title = Resources.cap_export_folder;
			folderDialog.IsFolderPicker = true;
			folderDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			folderDialog.EnsurePathExists = true;
			folderDialog.AllowNonFileSystemItems = false;
			folderDialog.EnsureFileExists = true;
			folderDialog.EnsureReadOnly = false;
			folderDialog.EnsureValidNames = true;
			folderDialog.Multiselect = false;
			folderDialog.AddToMostRecentlyUsedList = false;
			folderDialog.ShowPlacesList = true;

			if (folderDialog.ShowDialog() != WinAPICodePack.CommonFileDialogResult.Ok)
				return false;

			var outputDirectory = folderDialog.FileName;
			if (Directory.Exists(outputDirectory) == false)
				return false;

			AppSettings.Paths.LastImportExportPath = outputDirectory;
		
			var filenames = new List<string>(characterInstances.Length);
			HashSet<string> used_filenames = new HashSet<string>();
			if (formatDialog.FileFormat.Contains(FileUtil.FileType.Backup))
			{
				string now = DateTime.Now.ToString("yyyy-MM-dd");
				foreach (var character in characterInstances)
				{
					filenames.Add(Utility.MakeUniqueFilename(outputDirectory,
						string.Format("{0}_{1}_{2}.backup.zip",
							character.displayName.Replace(" ", "_"),
							character.creationDate.ToFileTimeUtc() / 1000L,
							now),
						used_filenames)
					);
				}
			}
			else
			{
				foreach (var character in characterInstances)
				{
					filenames.Add(Utility.MakeUniqueFilename(outputDirectory,
						string.Format("{0}_{1}.{2}",
							character.displayName,
							character.creationDate.ToFileTimeUtc() / 1000L,
							ext),
						used_filenames)
					);
				}
			}

			// Confirm overwrite?
			bool bFileExists = filenames.ContainsAny(fn => File.Exists(fn));
			if (bFileExists && MsgBox.Confirm(Resources.msg_link_export_overwrite_files, Resources.cap_overwrite_files) == false)
				return false;

			var exporter = new BulkExporter();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Exporting...";

			progressDlg.onCancel += (s, e) => {
				exporter.Cancel();
				progressDlg.Close();
			};
			exporter.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			exporter.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteExport(result, filenames);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < characterInstances.Length && i < filenames.Count; ++i)
				exporter.Enqueue(characterInstances[i], filenames[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			exporter.Start(formatDialog.FileFormat);
			progressDlg.ShowDialog(this);

			return true;
		}
		
		public bool ExportManyPartiesFromBackyard()
		{
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats) == false)
				return false; // Error

			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_overwrite_files);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleCharactersOrGroupsDialog();
			dlg.Options = LinkSelectMultipleCharactersOrGroupsDialog.Option.Parties;
			dlg.Text = "Select parties to export";

			if (dlg.ShowDialog() != DialogResult.OK || dlg.SelectedGroups.Length == 0)
				return false;

			GroupInstance[] groupInstances = dlg.SelectedGroups;

			// Export format
			var formatDialog = new FileFormatDialog();
			formatDialog.GroupFormats = true;
			if (formatDialog.ShowDialog() != DialogResult.OK)
				return false;

			string ext;
			if (formatDialog.FileFormat.Contains(FileUtil.FileType.Png))
				ext = "png";
			else if (formatDialog.FileFormat.Contains(FileUtil.FileType.Backup))
				ext = "zip";
			else
				return false; // Error

			var folderDialog = new WinAPICodePack.CommonOpenFileDialog();
			folderDialog.Title = Resources.cap_export_folder;
			folderDialog.IsFolderPicker = true;
			folderDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			folderDialog.EnsurePathExists = true;
			folderDialog.AllowNonFileSystemItems = false;
			folderDialog.EnsureFileExists = true;
			folderDialog.EnsureReadOnly = false;
			folderDialog.EnsureValidNames = true;
			folderDialog.Multiselect = false;
			folderDialog.AddToMostRecentlyUsedList = false;
			folderDialog.ShowPlacesList = true;

			if (folderDialog.ShowDialog() != WinAPICodePack.CommonFileDialogResult.Ok)
				return false;

			var outputDirectory = folderDialog.FileName;
			if (Directory.Exists(outputDirectory) == false)
				return false;

			AppSettings.Paths.LastImportExportPath = outputDirectory;
		
			var filenames = new List<string>(groupInstances.Length);
			HashSet<string> used_filenames = new HashSet<string>();
			if (formatDialog.FileFormat.Contains(FileUtil.FileType.Backup))
			{
				string now = DateTime.Now.ToString("yyyy-MM-dd");
				foreach (var group in groupInstances)
				{
					filenames.Add(Utility.MakeUniqueFilename(outputDirectory,
						string.Format("{0}_{1}_{2}.backup.zip",
							group.displayName.Replace(" ", "_"),
							group.creationDate.ToFileTimeUtc() / 1000L,
							now),
						used_filenames)
					);
				}
			}
			else
			{
				foreach (var group in groupInstances)
				{
					filenames.Add(Utility.MakeUniqueFilename(outputDirectory,
						string.Format("{0}_{1}.{2}",
							group.displayName,
							group.creationDate.ToFileTimeUtc() / 1000L,
							ext),
						used_filenames)
					);
				}
			}

			// Confirm overwrite?
			bool bFileExists = filenames.ContainsAny(fn => File.Exists(fn));
			if (bFileExists && MsgBox.Confirm(Resources.msg_link_export_overwrite_files, Resources.cap_overwrite_files) == false)
				return false;

			var exporter = new BulkExporter();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Exporting...";

			progressDlg.onCancel += (s, e) => {
				exporter.Cancel();
				progressDlg.Close();
			};
			exporter.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			exporter.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteExport(result, filenames);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < groupInstances.Length && i < filenames.Count; ++i)
				exporter.Enqueue(groupInstances[i], filenames[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			exporter.Start(formatDialog.FileFormat);
			progressDlg.ShowDialog(this);
			return true;
		}

		private void CompleteExport(BulkExporter.Result result, List<string> filenames)
		{
			if (result.error == BulkExporter.Error.NoError)
			{
				int succeeded = result.filenames.Count;
				int skipped = filenames.Count - succeeded;
				if (skipped > 0)
				{
					MsgBox.Message(string.Format(Resources.msg_link_export_some_characters, NumCharacters(succeeded), skipped), Resources.cap_link_export_many_characters);
				}
				else
				{
					MsgBox.Message(string.Format(Resources.msg_link_export_many_characters, NumCharacters(succeeded)), Resources.cap_link_export_many_characters);
				}
			}
			else if (result.error == BulkExporter.Error.Cancelled)
			{
				MsgBox.LinkError.Canceled(Resources.cap_link_export_many_characters);
			}
			else if (result.error == BulkExporter.Error.FileError)
			{
				MsgBox.Error(Resources.error_write_file, Resources.cap_link_export_many_characters);
			}
			else if (result.error == BulkExporter.Error.DiskFullError)
			{
				MsgBox.Error(Resources.error_disk_full, Resources.cap_link_export_many_characters);
			}
			else
			{
				MsgBox.Error(Resources.error_link_export_many_characters, Resources.cap_link_export_many_characters);
			}
		}

		public bool ImportManyToBackyard()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_import_many_characters);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Select files...
			try
			{
				importFileDialog.Title = Resources.cap_import_character;
				importFileDialog.Filter = "All supported types|*.png;*.json;*.charx;*.yaml;*.zip|PNG files|*.png|JSON files|*.json|CHARX files|*.charx|YAML files|*.yaml|Character backup files|*.zip";
				importFileDialog.FilterIndex = AppSettings.User.LastImportCharacterFilter;
				importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
				importFileDialog.Multiselect = true;
				importFileDialog.FileName = "";

				var mr = importFileDialog.ShowDialog();
				if (mr != DialogResult.OK || importFileDialog.FileNames.Length == 0)
					return false;

				AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileNames[0]);
				AppSettings.User.LastImportCharacterFilter = importFileDialog.FilterIndex;
			}
			finally
			{
				importFileDialog.Multiselect = false;
			}

			// Identify file types and import (no progress bar)
			var filenames = importFileDialog.FileNames.ToArray();
			if (filenames.Length < 10)
			{ 
				filenames = filenames
					.Where(fn => FileUtil.CheckFileType(fn) != FileUtil.FileType.Unknown)
					.OrderBy(fn => new FileInfo(fn).LastWriteTime)
					.ToArray();
				return BeginImport(filenames);
			}

			// Identify file types (with progress bar)
			var checker = new AsyncFileTypeChecker();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Identifying file types...";

			progressDlg.onCancel += (s, e) => {
				checker.Cancel();
				progressDlg.Close();
				MsgBox.LinkError.Canceled(Resources.cap_link_import_many_characters);
			};
			checker.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			checker.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				if (result.error == AsyncFileTypeChecker.Error.NoError)
				{
					BeginImport(result.filenames);
				}
				else if (result.error == AsyncFileTypeChecker.Error.Cancelled)
				{
					MsgBox.LinkError.Canceled(Resources.cap_link_import_many_characters);
				}
			};

			checker.Enqueue(filenames);
			checker.Start();
			progressDlg.ShowDialog(this);

			return true;
		}

		private bool BeginImport(string[] filenames)
		{
			if (filenames.Length == 0)
			{
				MsgBox.Error(Resources.error_link_import_many_unsupported, Resources.cap_link_import_many_characters);
				return false;
			}

			// Confirm
			if (MsgBox.Confirm(string.Format(Resources.msg_link_confirm_import_many, NumCharacters(filenames.Length)), Resources.cap_link_import_many_characters) == false)
				return false;

			// Create Ginger import folder
			FolderInstance importFolder;
			if (string.IsNullOrEmpty(AppSettings.BackyardLink.BulkImportFolderName) == false)
			{
				string folderName = AppSettings.BackyardLink.BulkImportFolderName.Trim();
				string folderUrl = BackyardUtil.ToFolderUrl(folderName);
				importFolder = Backyard.Folders
					.Where(f => string.Compare(f.name, folderName, StringComparison.OrdinalIgnoreCase) == 0
						|| string.Compare(f.url, folderUrl, StringComparison.OrdinalIgnoreCase) == 0)
					.FirstOrDefault();

				if (importFolder.isEmpty)
					Backyard.Database.CreateNewFolder(folderName, out importFolder); // It's ok if this fails.
			}
			else
				importFolder = default(FolderInstance);

			var importer = new BulkImporter();
			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Importing...";

			progressDlg.onCancel += (s, e) => {
				importer.Cancel();
				progressDlg.Close();
			};
			importer.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			importer.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteImport(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < filenames.Length; ++i)
				importer.Enqueue(filenames[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			importer.Start(importFolder);
			progressDlg.ShowDialog(this);
			return true;
		}

		private void CompleteImport(BulkImporter.Result result)
		{
			if (result.error == BulkImporter.Error.NoError)
			{
				MsgBox.Message(string.Format(result.skipped == 0 ? Resources.msg_link_import_many_characters : Resources.msg_link_import_some_characters, NumCharacters(result.succeeded), result.skipped), Resources.cap_link_import_many_characters);
			}
			else if (result.error == BulkImporter.Error.Cancelled)
			{
				MsgBox.LinkError.Canceled(Resources.cap_link_import_many_characters);
			}
			else
			{
				MsgBox.Error(Resources.error_link_import_many_characters, Resources.cap_link_import_many_characters);
			}
		}

		private bool EditManyModelSettings()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_update_many_characters);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleCharactersOrGroupsDialog();
			dlg.Options = LinkSelectMultipleCharactersOrGroupsDialog.Option.Solo;
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
				dlg.Options |= LinkSelectMultipleCharactersOrGroupsDialog.Option.Parties;
			dlg.Text = "Select chats to modify";
			
			if (dlg.ShowDialog() != DialogResult.OK || dlg.SelectedGroups.Length == 0)
				return false;

			var groupInstances = dlg.SelectedGroups;

			// Model settings
			var dlgSettings = new EditModelSettingsDialog();
			if (dlgSettings.ShowDialog() != DialogResult.OK)
				return false;

			// Confirm
			if (MsgBox.Confirm(string.Format(Resources.msg_link_confirm_update_many, NumGroups(groupInstances.Length)), Resources.cap_link_update_many_characters) == false)
				return false;

			var updater = new BulkUpdateModelSettings();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Updating...";

			progressDlg.onCancel += (s, e) => {
				updater.Cancel();
				progressDlg.Close();
			};
			updater.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			updater.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteUpdateSettings(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < groupInstances.Length; ++i)
				updater.Enqueue(groupInstances[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			updater.Start(dlgSettings.Parameters);
			progressDlg.ShowDialog(this);

			return true;
		}

		private void CompleteUpdateSettings(BulkUpdateModelSettings.Result result)
		{
			if (result.error == BulkUpdateModelSettings.Error.NoError)
			{
				MsgBox.Message(string.Format(result.skipped == 0 ? Resources.msg_link_update_many_characters : Resources.msg_link_update_some_characters, NumCharacters(result.succeeded), result.skipped), Resources.cap_link_update_many_characters);
			}
			else if (result.error == BulkUpdateModelSettings.Error.Cancelled)
			{
				MsgBox.LinkError.Canceled(Resources.cap_link_update_many_characters);
			}
			else
			{
				MsgBox.Error(Resources.error_link_update_many_characters, Resources.cap_link_update_many_characters);
			}
		}

		private bool CreateBackyardBackup()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_create_backup);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			var dlg = new LinkSelectCharacterOrGroupDialog();
			dlg.Text = Resources.cap_link_create_backup;
			dlg.Options = LinkSelectCharacterOrGroupDialog.Option.Solo;
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
				dlg.Options |= LinkSelectCharacterOrGroupDialog.Option.Parties;

			GroupInstance groupInstance;
			if (dlg.ShowDialog() == DialogResult.OK)
				groupInstance = dlg.SelectedGroup;
			else
				return false;

			if (string.IsNullOrEmpty(groupInstance.instanceId))
				return false; // Error

			BackupData backup = null;
			var error = RunTask(() => BackupUtil.CreateBackup(groupInstance, out backup), "Creating backup...");
			if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_create_backup, Resources.cap_link_create_backup);
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Error(error, Resources.cap_link_create_backup);
				return false;
			}

			string filename = string.Concat(groupInstance.displayName.Replace(" ", "_"), " - ", DateTime.Now.ToString("yyyy-MM-dd"), ".backup.zip");

			importFileDialog.Title = Resources.cap_link_create_backup;
			exportFileDialog.Filter = "Character backup file|*.zip";
			exportFileDialog.FileName = Utility.ValidFilename(filename);
			exportFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			exportFileDialog.FilterIndex = AppSettings.User.LastExportChatFilter;

			var result = exportFileDialog.ShowDialog();
			if (result != DialogResult.OK || string.IsNullOrWhiteSpace(exportFileDialog.FileName))
				return false;

			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(exportFileDialog.FileName);
			AppSettings.User.LastExportChatFilter = exportFileDialog.FilterIndex;

			if (BackupUtil.WriteBackup(exportFileDialog.FileName, backup) != FileUtil.Error.NoError)
			{
				MsgBox.Error(Resources.error_write_file, Resources.cap_link_create_backup);
				return false;
			}

			MsgBox.Message(Resources.msg_link_create_backup, Resources.cap_link_create_backup);
			return true;
		}

		private bool RestoreBackyardBackup()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_restore_backup);
				return false;
			}

			importFileDialog.Title = Resources.cap_link_restore_backup;
			importFileDialog.Filter = "Character backup file|*.zip";
			importFileDialog.FilterIndex = AppSettings.User.LastImportChatFilter;
			importFileDialog.InitialDirectory = AppSettings.Paths.LastImportExportPath ?? AppSettings.Paths.LastCharacterPath ?? Utility.AppPath("Characters");
			var result = importFileDialog.ShowDialog();
			if (result != DialogResult.OK)
				return false;

			AppSettings.User.LastImportChatFilter = importFileDialog.FilterIndex;
			AppSettings.Paths.LastImportExportPath = Path.GetDirectoryName(importFileDialog.FileName);

			BackupData backup;
			FileUtil.Error readError = BackupUtil.ReadBackup(importFileDialog.FileName, out backup);
			if (readError != FileUtil.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_restore_backup_invalid, Resources.cap_link_restore_backup);
				return false;
			}

			if (backup.characterCards.Length > 1 && BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats) == false)
			{
				MsgBox.Error(Resources.error_link_restore_backup_unsupported, Resources.cap_link_restore_backup);
				return false;
			}

			// Confirmation
			if (MsgBox.Confirm(string.Format(Resources.msg_link_restore_backup, backup.displayName, backup.chats.Count), Resources.cap_link_restore_backup) == false)
				return false;

			// Import model settings?
			if (backup.hasModelSettings 
				&& MsgBox.Ask(Resources.msg_link_restore_backup_settings, Resources.cap_link_restore_backup) == false)
			{
				// Reset model parameters
				foreach (var chat in backup.chats)
					chat.parameters = AppSettings.BackyardSettings.UserSettings;
			}

			List<Backyard.ImageInput> images = new List<Backyard.ImageInput>();
			images.AddRange(backup.images
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						actorIndex = i.characterIndex,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Icon,
					},
					fileExt = i.ext,
				}));

			images.AddRange(backup.backgrounds
				.Select(i => new Backyard.ImageInput {
					asset = new AssetFile() {
						name = i.filename,
						data = AssetData.FromBytes(i.data),
						ext = i.ext,
						assetType = AssetFile.AssetType.Background,
					},
					fileExt = i.ext,
				}));

			if (backup.userPortrait != null && backup.userPortrait.data != null && backup.userPortrait.data.Length > 0)
			{
				images.Add(new Backyard.ImageInput {
					asset = new AssetFile() {
						name = backup.userPortrait.filename,
						data = AssetData.FromBytes(backup.userPortrait.data),
						ext = backup.userPortrait.ext,
						assetType = AssetFile.AssetType.UserIcon,
					},
					fileExt = backup.userPortrait.ext,
				});
			}

			// Create Ginger import folder
			FolderInstance backupFolder;
			if (string.IsNullOrEmpty(AppSettings.BackyardLink.BulkImportFolderName) == false)
			{
				string folderName = "Restored from backup";
				string folderUrl = BackyardUtil.ToFolderUrl(folderName);
				backupFolder = Backyard.Folders
					.Where(f => string.Compare(f.name, folderName, StringComparison.OrdinalIgnoreCase) == 0
						|| string.Compare(f.url, folderUrl, StringComparison.OrdinalIgnoreCase) == 0)
					.FirstOrDefault();

				if (backupFolder.isEmpty)
					Backyard.Database.CreateNewFolder(folderName, out backupFolder); // It's ok if this fails.
			}
			else
				backupFolder = default(FolderInstance);

			// Write character
			var args = new Backyard.CreatePartyArguments() {
				cards = backup.characterCards.ToArray(),
				imageInput = images.ToArray(),
				chats = backup.chats.ToArray(),
				userInfo = backup.userInfo,
				folder = backupFolder,
			};
			Backyard.Link.Image[] imageLinks; // Ignored
			GroupInstance groupInstance;
			CharacterInstance[] returnedCharacters;
			Backyard.Error error = RunTask(() => Backyard.Database.CreateNewParty(args, out groupInstance, out returnedCharacters, out imageLinks), "Restoring backup...");
			if (error != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Error(error, Resources.cap_link_restore_backup);
				return false;
			}
						
			MsgBox.Message(Resources.msg_link_restore_backup_success, Resources.cap_link_restore_backup);
			return true;
		}

		private void RepairBrokenImages()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_repair_images);
				return;
			}

			if (MsgBox.Confirm(Resources.msg_link_repair_images, Resources.cap_link_repair_images) == false)
				return;

			int modified = 0;
			int skipped = 0;
			var error = RunTask(() => Backyard.Database.RepairImages(out modified, out skipped), "Repairing broken images...");
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_repair_images);
				return;
			}
			if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_images_folder_not_found, Resources.cap_link_repair_images);
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_repair_images, Resources.cap_link_repair_images);
				return;
			}

			// Success
			if (skipped > 0)
			{
				MsgBox.Message(string.Format(Resources.msg_link_repaired_images_skipped, modified, skipped), Resources.cap_link_repair_images);
			}
			else if (modified > 0)
			{
				MsgBox.Message(string.Format(Resources.msg_link_repaired_images, modified), Resources.cap_link_repair_images);
			}
			else
			{
				MsgBox.Message(string.Format(Resources.msg_link_no_images_repaired, modified), Resources.cap_link_repair_images);
			}
		}

		private void PurgeUnusedImages()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_purge_images);
				return;
			}

			var imagesFolder = Path.Combine(AppSettings.BackyardLink.Location, "images");
			if (Directory.Exists(imagesFolder) == false)
			{
				MsgBox.Error(Resources.error_link_images_folder_not_found, Resources.cap_link_purge_images);
				return;
			}

			string[] imageUrls = new string[0];

			var error = RunTask(() => Backyard.Database.GetAllImageUrls(out imageUrls));

			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_purge_images);
				return;
			}
			if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_repair_images, Resources.cap_link_purge_images);
				return;
			}
			if (imageUrls == null || imageUrls.Length == 0)
			{
				MsgBox.Message(Resources.msg_link_purge_images_not_found, Resources.cap_link_purge_images);
				return;
			}

			var images = new HashSet<string>(imageUrls
				.Select(fn => Path.GetFileName(fn).ToLowerInvariant())
				.Where(fn => string.IsNullOrEmpty(fn) == false));
			
			var foundImageFilenames = new HashSet<string>(Utility.FindFilesInFolder(imagesFolder)
				.Select(fn => Path.GetFileName(fn).ToLowerInvariant())
				.Where(fn => {
					return Utility.IsSupportedImageFilename(fn);
				}));

			var unknownImages = foundImageFilenames.Except(images)
				.Select(fn => Path.Combine(imagesFolder, fn))
				.ToList();

			if (unknownImages.Count > 0)
			{
				if (MsgBox.Confirm(string.Format(Resources.msg_link_purge_images_confirm, unknownImages.Count), Resources.cap_link_purge_images))
				{
					Win32.SendToRecycleBin(unknownImages, Win32.FileOperationFlags.FOF_WANTNUKEWARNING | Win32.FileOperationFlags.FOF_NOCONFIRMATION);
				}
			}
			else
			{
				MsgBox.Message(Resources.msg_link_purge_images_not_found, Resources.cap_link_purge_images);
			}
		}

		private bool EditCurrentModelSettings()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_edit_model_settings);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			GroupInstance groupInstance;
			if (Current.Link.groupId != null)
				groupInstance = Backyard.Database.GetGroup(Current.Link.groupId);
			else
				groupInstance = Backyard.Database.GetGroupForCharacter(Current.Link.mainActorId);

			if (groupInstance.isDefined == false)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_edit_model_settings);
				Current.BreakLink();
				return false;
			}

			ChatInstance[] chats = null;
			if (RunTask(() => Backyard.Database.GetChats(groupInstance.instanceId, out chats)) != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_edit_model_settings);
				return false;
			}

			if (chats == null || chats.Length == 0)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_edit_model_settings);
				return false;
			}

			// Model settings dialog
			var dlg = new EditModelSettingsDialog();
			dlg.Editing = chats[0].parameters;
			if (dlg.ShowDialog() != DialogResult.OK)
				return false;

			string[] chatIds = chats.Select(c => c.instanceId).ToArray();

			var error = RunTask(() => Backyard.Database.UpdateChatParameters(chatIds, dlg.Parameters, null), "Updating model settings...");
			if (error == Backyard.Error.NotFound)
			{
				MsgBox.Error(Resources.error_link_chat_not_found, Resources.cap_link_edit_model_settings);
				return false;;
			}
			if (error != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Error(error, Resources.cap_link_edit_model_settings);
				return false;
			}

			SetStatusBarMessage(Resources.status_link_update_model_settings, Constants.StatusBarMessageInterval);
			return true;
		}

		private bool RepairLegacyChats()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_bulk_repair_chats);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			var groups = Backyard.Groups.ToArray();

			// Confirm
			if (MsgBox.Confirm(Resources.msg_link_bulk_repair_chats_confirm, Resources.cap_link_bulk_repair_chats) == false)
				return false;

			var updater = new LegacyChatUpdater();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Repairing...";

			progressDlg.onCancel += (s, e) => {
				updater.Cancel();
				progressDlg.Close();
			};
			updater.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			updater.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteRepairLegacyChats(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			updater.Enqueue(groups);

			_bCanRegenerate = false;
			_bCanIdle = false;
			updater.Start();
			progressDlg.ShowDialog(this);
			return true;
		}

		private void CompleteRepairLegacyChats(LegacyChatUpdater.Result result)
		{
			if (result.error == LegacyChatUpdater.Error.NoError)
			{
				if (result.numCharacters > 0)
				{
					MsgBox.Message(string.Format(Resources.msg_link_bulk_repair_chats, result.numChats, result.numCharacters), Resources.cap_link_bulk_repair_chats);
				}
				else
				{
					MsgBox.Message(Resources.msg_link_bulk_repair_chats_none, Resources.cap_link_bulk_repair_chats);
				}
				
			}
			else if (result.error == LegacyChatUpdater.Error.Cancelled)
			{
				MsgBox.LinkError.Canceled(Resources.cap_link_bulk_repair_chats);
			}
			else
			{
				MsgBox.Error(Resources.error_link_bulk_repair_chats, Resources.cap_link_bulk_repair_chats);
			}
		}

		private bool DeleteBackyardCharacters()
		{
			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.cap_link_delete_characters);
				AppSettings.BackyardLink.Enabled = false;
				return false;
			}

			// Choose character(s)
			var dlg = new LinkSelectMultipleCharactersOrGroupsDialog();
			dlg.Options = LinkSelectMultipleCharactersOrGroupsDialog.Option.Solo;
			dlg.Text = "Select characters to delete";
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyChats))
			{
				dlg.Options |= LinkSelectMultipleCharactersOrGroupsDialog.Option.Orphans | LinkSelectMultipleCharactersOrGroupsDialog.Option.Parties;
				dlg.Text = "Select characters or parties to delete";
			}

			if (dlg.ShowDialog() != DialogResult.OK)
				return false;

			// Combine and filter character ids
			var characterIds = dlg.SelectedCharacters
				.Select(c => c.instanceId)
				.Union(dlg.SelectedGroups.SelectMany(g => g.members))
				.Select(id => Backyard.Database.GetCharacter(id))
				.Where(c => c.isCharacter)
				.Select(c => c.instanceId)
				.Distinct()
				.ToArray();

			// Get affected character ids and group ids.
			Backyard.ConfirmDeleteResult result;
			Backyard.Error error = Backyard.Database.ConfirmDeleteCharacters(characterIds, out result);
			if (error != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Error(error, Resources.cap_link_delete_characters);
				return false;
			}

			// Confirm delete
			string confirmMessage = result.characterIds.Length != result.groupIds.Length ?
				string.Format(Resources.msg_link_delete_characters_and_group_chats_confirm, NumCharacters(result.characterIds.Length)) :
				string.Format(Resources.msg_link_delete_characters_confirm, NumCharacters(result.characterIds.Length));
			if (MsgBox.Confirm(confirmMessage, Resources.cap_link_delete_characters) == false)
				return false;

			error = RunTask(() => Backyard.Database.DeleteCharacters(result.characterIds, result.groupIds, result.imageIds), "Deleting characters...");
			if (error != Backyard.Error.NoError)
			{
				MsgBox.LinkError.Error(error, Resources.cap_link_delete_characters);
				return false;
			}

			// Delete orphaned users
			string[] imageUrls;
			Backyard.Database.DeleteOrphanedUsers(out imageUrls);
			imageUrls = Utility.ConcatenateArrays(imageUrls, result.imageUrls);

			// Delete image files
			try
			{
				foreach (var imageUrl in imageUrls)
				{
					if (File.Exists(imageUrl))
						File.Delete(imageUrl);
				}
			}
			catch 
			{ 
			}

			MsgBox.Message(string.Format(Resources.msg_link_deleted_characters, NumCharacters(result.characterIds.Length)), Resources.cap_link_delete_characters);
			
			return true;
		}

		private bool ImportActorFromFile(string filename)
		{
			if (Current.Characters.Count >= Constants.MaxActorCount)
				return false;

			var stash = Current.Stash();
			try
			{
				Undo.Suspend();
				Current.Instance = new GingerCharacter();
				Current.Reset();

				if (ImportCharacter(filename) == false)
				{
					Current.Restore(stash);
					return false;
				}

				// Get portrait
				var portraitImage = Current.Card.portraitImage;
				var portraitAsset = Current.Card.assets.GetPortraitOverride();

				// Add actor
				CharacterData importedCharacter = Current.Character;
				importedCharacter.spokenName = Current.Name;
				Current.Restore(stash);

				// Replace last (if empty)
				int lastIndex = Current.Characters.Count - 1;
				var lastCharacter = Current.Characters[lastIndex];
				if (lastCharacter.recipes.IsEmpty() && string.IsNullOrEmpty(lastCharacter.spokenName)
					&& ((lastIndex > 0 && Current.Card.assets.Count(a => a.actorIndex == lastIndex) == 0)
						|| (lastIndex == 0 && Current.Card.portraitImage == null && Current.Card.assets.Count(a => a.actorIndex <= 0) == 0)) )
				{
					Current.Characters.RemoveAt(lastIndex);
				}

				Current.Characters.Add(importedCharacter);
				Current.SelectedCharacter = Current.Characters.Count - 1;
				Current.IsDirty = true;

				// Add portrait
				if (Current.SelectedCharacter > 0 && portraitAsset == null && portraitImage != null)
					portraitAsset = AssetFile.FromImage(portraitImage);

				if (portraitAsset != null)
				{
					portraitAsset.name = string.Format("Portrait ({0})", Current.Character.spokenName);
					portraitAsset.actorIndex = Current.Characters.Count - 1;
					if (Current.SelectedCharacter > 0)
						portraitAsset.RemoveTags(AssetFile.Tag.PortraitOverride);
					Current.Card.assets.Add(portraitAsset);
				}
				if (Current.SelectedCharacter == 0) // Also set main portrait
				{
					if (portraitAsset != null)
						Current.Card.portraitImage = ImageRef.FromImage(portraitAsset.ToImage());
					else
						Current.Card.portraitImage = portraitImage;
				}

				Current.Card.assets.Validate();

				// Validate recipes
				Context context = Current.Character.GetContext(CharacterData.ContextType.FlagsOnly, Generator.Option.None, true);
				var evalCookie = new EvaluationCookie() { ruleSuppliers = Current.RuleSuppliers };
				Current.Character.recipes.RemoveAll(r => r.isBase || (r.requires != null && r.requires.Evaluate(context, evalCookie)));
			}
			finally
			{
				Undo.Resume();
			}
			return true;
		}

		private bool ExportCurrentActor()
		{
			var character = Current.Character.Clone();

			Image portraitImage = Current.Card.portraitImage;
			AssetFile portraitAsset = Current.Card.assets.GetPortraitOverride();
			if (Current.SelectedCharacter > 0)
			{
				portraitAsset = Current.Card.assets.GetPortrait(Current.SelectedCharacter);
				if (portraitAsset != null)
					portraitImage = portraitAsset.ToImage();
			}

			var stash = Current.Stash();
			try
			{
				Undo.Suspend();
				Current.Instance = new GingerCharacter();
				Current.Reset();
				Current.Characters.Clear();
				Current.Characters.Add(character);

				if (portraitAsset != null && portraitAsset.HasTag(AssetFile.Tag.Animation))
				{
					// Set portrait override
					portraitAsset = (AssetFile)portraitAsset.Clone();
					portraitAsset.AddTags(AssetFile.Tag.PortraitOverride);
					Current.Card.assets.Add(portraitAsset);
				}

				if (portraitImage != null)
					Current.Card.portraitImage = ImageRef.FromImage(portraitImage, false);

				return SaveAs();
			}
			finally
			{
				Current.Restore(stash);
				Undo.Resume();
			}
		}

		private bool SavePartyToBackyard()
		{
			GroupInstance createdGroup;
			CharacterInstance[] createdCharacters;
			Backyard.Link.Image[] images;

			var error = CreateNewPartyInBackyard(out createdGroup, out createdCharacters, out images);
			if (error == Backyard.Error.NotConnected)
			{
				MsgBox.LinkError.ConnectionFailed();
				return false;
			}
			else if (error != Backyard.Error.NoError)
			{
				MsgBox.Error(Resources.error_link_save_character_as_new, Resources.cap_link_save_character);
				return false;
			}
			else
			{
				if (AppSettings.BackyardLink.AlwaysLinkOnImport 
					|| MsgBox.Ask(Resources.msg_link_create_link, Resources.cap_link_character))
				{
					Current.LinkWith(createdGroup, createdCharacters, images);
					Current.IsLinkDirty = false;
					SetStatusBarMessage(Resources.status_link_save_and_link_new, Constants.StatusBarMessageInterval);
					RefreshTitle();
					MsgBox.Message(Resources.msg_link_save_and_link_new, Resources.cap_link_save_character);
				}
				else
				{
					MsgBox.Message(Resources.msg_link_saved, Resources.cap_link_save_character);
				}
				
				_bShouldRefreshSidePanel = true;
				return true;
			}
		}

		private Backyard.Error CreateNewPartyInBackyard(out GroupInstance createdGroup, out CharacterInstance[] createdCharacters, out Backyard.Link.Image[] images)
		{
			if (Backyard.ConnectionEstablished == false)
			{
				createdGroup = default(GroupInstance);
				createdCharacters = null;
				images = null;
				return Backyard.Error.NotConnected;
			}

			Generator.Option options = Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked | Generator.Option.Group;

			var outputs = Generator.GenerateMany(options);
			
			// User persona
			UserData userInfo = null;
			if (AppSettings.BackyardLink.WriteUserPersona)
			{
				string userPersona = outputs[0].userPersona.ToFaraday();
				if (string.IsNullOrEmpty(userPersona) == false)
				{
					userInfo = new UserData() {
						name = Current.Card.userPlaceholder,
						persona = userPersona,
					};
					outputs[0].userPersona = GingerString.Empty;
				}
			}

			FaradayCardV4[] cards = outputs.Select(o => FaradayCardV4.FromOutput(o)).ToArray();
			if (cards == null || cards.Length == 0)
			{
				createdGroup = default(GroupInstance);
				createdCharacters = null;
				images = null;
				return Backyard.Error.InvalidArgument; // Error
			}

			for (int i = 0; i < cards.Length && i < Current.Characters.Count; ++i)
				cards[i].data.name = Current.Characters[i].name;
			cards[0].EnsureSystemPrompt();
			cards[0].data.isNSFW = cards.ContainsAny(c => c.data.isNSFW);

			Backyard.ImageInput[] imageInput = BackyardUtil.GatherImages();
			BackupData.Chat[] chats = null;
//			if (AppSettings.BackyardLink.ImportAlternateGreetings && output.greetings.Length > 1) //! @party
//				chats = Backyard.Database.GatherChats(card, output, imageInput);
			
			var args = new Backyard.CreatePartyArguments() {
				cards = cards,
				imageInput = imageInput,
				chats = chats,
				userInfo = userInfo,
			};

			var error = Backyard.Database.CreateNewParty(args, out createdGroup, out createdCharacters, out images);
			if (error != Backyard.Error.NoError)
				return error;

			Current.IsFileDirty = true;
			Current.IsLinkDirty = false;
			RefreshTitle();

			// Refresh character information
			Backyard.RefreshCharacters();
			return Backyard.Error.NoError;
		}

		private Backyard.Error UpdateGroupInBackyard()
		{
			if (Backyard.ConnectionEstablished == false)
				return Backyard.Error.NotConnected;
			else if (Current.HasLink == false)
				return Backyard.Error.NotFound;

			// Refresh character list
			if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
			{
				MsgBox.LinkError.RefreshFailed(Resources.msg_link_confirm_overwrite);
				AppSettings.BackyardLink.Enabled = false;
			}

			// Check if character exists, has newer changes
			bool hasChanges;
			var error = Backyard.Database.ConfirmSaveCharacter(Current.Link, out hasChanges);
			if (error == Backyard.Error.NotFound)
			{
				Current.BreakLink();
				return error;
			}
			if (error != Backyard.Error.NoError)
				return error;

			Generator.Option options = Generator.Option.Export | Generator.Option.Faraday | Generator.Option.Linked;
			if (BackyardValidation.CheckFeature(BackyardValidation.Feature.PartyNames))
				options |= Generator.Option.Group;

			var outputs = Generator.GenerateMany(options);

			// User persona
			UserData userInfo = null;
			if (AppSettings.BackyardLink.WriteUserPersona)
			{
				string userPersona = outputs[0].userPersona.ToFaraday();
				if (string.IsNullOrEmpty(userPersona) == false)
				{
					userInfo = new UserData() {
						name = Current.Card.userPlaceholder,
						persona = userPersona,
					};
					outputs[0].userPersona = GingerString.Empty;
				}
			}

			FaradayCardV4[] cards = outputs.Select(o => FaradayCardV4.FromOutput(o)).ToArray();
			if (cards == null || cards.Length == 0)
				return Backyard.Error.InvalidArgument; // Error

			for (int i = 0; i < cards.Length && i < Current.Characters.Count; ++i)
				cards[i].data.name = Current.Characters[i].name;
			cards[0].data.isNSFW = cards.ContainsAny(c => c.data.isNSFW);

			if (hasChanges)
			{
				// Overwrite prompt
				var mr = MsgBox.ConfirmYesNoCancel(Resources.msg_link_confirm_overwrite, Resources.cap_link_overwrite);
				if (mr == DialogResult.Cancel)
					return Backyard.Error.CancelledByUser;
				else if (mr == DialogResult.No)
					return Backyard.Error.DismissedByUser;
			}

			DateTime updateDate;
			Backyard.Link.Image[] imageLinks;
			error = Backyard.Database.UpdateParty(Current.Link, cards, userInfo, out updateDate, out imageLinks);
			if (error != Backyard.Error.NoError)
				return error;

			Current.Link.updateDate = updateDate;
			Current.Link.imageLinks = imageLinks;
			Current.IsFileDirty = true;
			Current.IsLinkDirty = false;
			RefreshTitle();

			// Refresh character information
			Backyard.RefreshCharacters();
			return Backyard.Error.NoError;
		}
			
		private bool ResetBackyardModelSettings()
		{
			// Refresh character list
            if (Backyard.RefreshCharacters() != Backyard.Error.NoError)
            {
                MsgBox.LinkError.RefreshFailed(Resources.cap_link_reset_model_settings);
                AppSettings.BackyardLink.Enabled = false;
                return false;
            }

			// Choose character(s)
			var groups = Backyard.Groups.ToArray();

			// Confirm
			if (MsgBox.Confirm(Resources.msg_link_reset_model_settings, Resources.cap_link_reset_model_settings) == false)
				return false;

			var updater = new BulkUpdateModelSettings();

			var progressDlg = new ProgressBarDialog();
			progressDlg.Message = "Updating...";

			progressDlg.onCancel += (s, e) => {
				updater.Cancel();
				progressDlg.Close();
			};
			updater.onProgress += (value) => {
				progressDlg.Percentage = value;
			};
			updater.onComplete += (result) => {
				progressDlg.Percentage = 100;
				progressDlg.TopMost = false;
				progressDlg.Close();

				CompleteResetModelSettings(result);
				_bCanRegenerate = true;
				_bCanIdle = true;
			};

			for (int i = 0; i < groups.Length; ++i)
				updater.Enqueue(groups[i]);

			_bCanRegenerate = false;
			_bCanIdle = false;
			updater.Start(new Backyard.ChatParameters());
			progressDlg.ShowDialog(this);

			return true;
		}

		private void CompleteResetModelSettings(BulkUpdateModelSettings.Result result)
		{
			if (result.error == BulkUpdateModelSettings.Error.NoError)
			{
				MsgBox.Message(string.Format(result.skipped == 0 ? Resources.msg_link_update_many_characters : Resources.msg_link_update_some_characters, NumCharacters(result.succeeded), result.skipped), Resources.cap_link_reset_model_settings);
			}
			else if (result.error == BulkUpdateModelSettings.Error.Cancelled)
			{
				MsgBox.LinkError.Canceled(Resources.cap_link_reset_model_settings);
			}
			else
			{
				MsgBox.Error(Resources.error_link_update_many_characters, Resources.cap_link_reset_model_settings);
			}
		}

		private bool ResetBackyardModelsLocation()
		{
			if (Backyard.ConnectionEstablished == false)
			{
				MsgBox.LinkError.Disconnected(Resources.cap_link_purge_images);
				return false;
			}

			if (string.IsNullOrEmpty(BackyardModelDatabase.ModelDownloadPath) == false
				&& Directory.Exists(BackyardModelDatabase.ModelDownloadPath))
			{
				if (MsgBox.Confirm(Resources.msg_link_repair_models_location_exists, Resources.cap_link_repair_models_location) == false)
					return false;
			}
			else
			{
				// Confirm
				if (MsgBox.Confirm(Resources.msg_link_repair_models_location, Resources.cap_link_repair_models_location) == false)
					return false;
			}

			var error = Backyard.Database.ResetModelDownloadLocation();
			if (error == Backyard.Error.NoError)
			{
				MsgBox.Message(Resources.msg_link_repair_models_location_success, Resources.cap_link_repair_models_location);
			}
			else
			{
				MsgBox.LinkError.Error(error, Resources.cap_link_repair_models_location);
			}


			return true;
		}
	}
}
