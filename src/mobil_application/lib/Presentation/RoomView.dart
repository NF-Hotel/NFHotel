import 'package:flutter/material.dart';
import 'package:mobil_application/Model/Enums/room_status.dart';
import 'package:mobil_application/Model/roomModel.dart';
import 'package:mobil_application/Controllers/roomController.dart';
import 'package:mobil_application/Widgets/RoomNotes.dart';
import 'package:mobil_application/Widgets/StatusIcon.dart';
import 'package:mobil_application/theme.dart';

class RoomView extends StatelessWidget {
  final RoomModel room;

  const RoomView({super.key, required this.room});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Room ${room.number}')),
      body: SafeArea(
        child: ListenableBuilder(
          listenable: roomController,
          builder: (context, _) => ListView(
            padding: const EdgeInsets.all(16),
            children: [
              for (final status in RoomStatus.values)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Card(
                    child: ListTile(
                      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                      leading: StatusIcon(status: status, size: 28),
                      title: Text(status.label),
                      selected: room.status == status,
                      trailing: room.status == status
                          ? const Icon(Icons.check, color: AppColors.navy)
                          : null,
                      onTap: () => roomController.setRoomStatus(room, status),
                    ),
                  ),
                ),
              RoomNotes(roomNumber: room.number),
            ],
          ),
        ),
      ),
    );
  }
}
