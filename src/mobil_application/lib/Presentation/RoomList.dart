import 'package:flutter/material.dart';
import 'package:mobil_application/Authentication/auth_session.dart';
import 'package:mobil_application/Controllers/roomController.dart';
import 'package:mobil_application/Presentation/LoginScreen.dart';
import 'package:mobil_application/Presentation/AdministrationPanel.dart';
import 'package:mobil_application/Presentation/RoomView.dart';
import 'package:mobil_application/Repositories/room_repository.dart';
import 'package:mobil_application/Widgets/RoomTile.dart';

class RoomOverview extends StatefulWidget {
  const RoomOverview({super.key});

  @override
  State<RoomOverview> createState() => _RoomOverviewState();
}

class _RoomOverviewState extends State<RoomOverview> {
  @override
  void initState() {
    super.initState();
    _loadRooms();
  }

  Future<void> _loadRooms() async {
    try {
      await roomController.loadRooms();
    } on RoomRepositoryException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.message)));
      }
    }
  }

  void _logout(BuildContext context) {
    AuthSession.token = null;
    roomController.clear();
    Navigator.of(context).pushAndRemoveUntil(
      MaterialPageRoute(builder: (context) => const LoginScreen()),
      (route) => false,
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: AuthSession.isAdmin
            ? IconButton(
                icon: const Icon(Icons.admin_panel_settings),
                tooltip: 'Administration',
                onPressed: () => Navigator.push(
                  context,
                  MaterialPageRoute(builder: (context) => const AdministrationPanel()),
                ),
              )
            : null,
        centerTitle: true,
        title: Text(AuthSession.displayName, overflow: TextOverflow.ellipsis),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Log out',
            onPressed: () => _logout(context),
          ),
        ],
      ),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: roomController,
          builder: (context, _) {
            final rooms = roomController.rooms;
            // pull to refresh picks up changes from other phones and new assignments
            return RefreshIndicator(
              onRefresh: _loadRooms,
              child: rooms.isEmpty && roomController.loaded
                  ? ListView(
                      padding: const EdgeInsets.all(32),
                      children: const [
                        Center(child: Text('No rooms to show.')),
                      ],
                    )
                  // 2 columns on phones, more on wider screens
                  : GridView.builder(
                      padding: const EdgeInsets.all(16),
                      gridDelegate:
                          const SliverGridDelegateWithMaxCrossAxisExtent(
                            maxCrossAxisExtent: 200,
                            childAspectRatio: 0.9,
                            mainAxisSpacing: 12,
                            crossAxisSpacing: 12,
                          ),
                      itemCount: rooms.length,
                      itemBuilder: (context, i) => RoomTile(
                        room: rooms[i],
                        onTap: () async {
                          await Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (context) => RoomView(room: rooms[i]),
                            ),
                          );
                          _loadRooms();
                        },
                      ),
                    ),
            );
          },
        ),
      ),
    );
  }
}
