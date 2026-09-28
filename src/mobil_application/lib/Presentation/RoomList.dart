import 'package:flutter/material.dart';
import 'package:mobil_application/Authentication/auth_session.dart';
import 'package:mobil_application/Controllers/roomController.dart';
import 'package:mobil_application/Presentation/LoginScreen.dart';
import 'package:mobil_application/Presentation/AdministrationPanel.dart';
import 'package:mobil_application/Presentation/RoomView.dart';
import 'package:mobil_application/Widgets/RoomTile.dart';

class RoomOverview extends StatelessWidget {
  const RoomOverview({super.key});

  void _logout(BuildContext context) {
    AuthSession.token = null;
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
          // 2 columns on phones, more on wider screens
          builder: (context, _) => GridView.builder(
            padding: const EdgeInsets.all(16),
            gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
              maxCrossAxisExtent: 200,
              childAspectRatio: 0.9,
              mainAxisSpacing: 12,
              crossAxisSpacing: 12,
            ),
            itemCount: roomController.rooms.length,
            itemBuilder: (context, i) => RoomTile(
              room: roomController.rooms[i],
              onTap: () => Navigator.push(
                context,
                MaterialPageRoute(
                  builder: (context) => RoomView(room: roomController.rooms[i]),
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
