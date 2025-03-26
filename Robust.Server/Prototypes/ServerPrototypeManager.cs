using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Server.Console;
using Robust.Server.Player;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Network.Messages;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Robust.Server.Prototypes
{
    public sealed class ServerPrototypeManager : PrototypeManager
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IConGroupController _conGroups = default!;
        [Dependency] private readonly INetManager _netManager = default!;
        [Dependency] private readonly IBaseServerInternal _server = default!;

        public ServerPrototypeManager()
        {
            RegisterIgnore("shader");
            RegisterIgnore("uiTheme");
            RegisterIgnore("font");
        }

        public override void Initialize()
        {
            base.Initialize();

            _netManager.RegisterNetMessage<MsgReloadPrototypes>(HandleReloadPrototypes, NetMessageAccept.Server);
        }

        private void HandleReloadPrototypes(MsgReloadPrototypes msg)
        {
#if TOOLS
            if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var player) ||
                !_conGroups.CanAdminReloadPrototypes(player))
            {
                return;
            }

            var sw = Stopwatch.StartNew();

            ReloadPrototypes(msg.Paths);

            Sawmill.Info($"Reloaded prototypes in {sw.ElapsedMilliseconds} ms");
#endif
        }

        public override void LoadDefaultPrototypes(Dictionary<Type, HashSet<string>>? changed = null)
        {
            LoadDirectory(new("/EnginePrototypes/"), changed: changed);
            LoadDirectory(_server.Options.PrototypeDirectory, changed: changed);
            foreach (var prototypeDirectory in _server.Options.OtherPrototypeDirectories)
            {
                Sawmill.Debug("Server: Trying to load from " + prototypeDirectory);
                LoadDirectory(prototypeDirectory, changed: changed);
            }
            ResolveResults();
        }

        public override void AddDirectory(string path)
        {
            if (path == string.Empty)
                return;
            var resPath = new ResPath(path);
            _server.Options.OtherPrototypeDirectories.Add(resPath);
        }
    }
}
