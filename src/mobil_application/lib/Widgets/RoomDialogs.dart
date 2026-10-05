import 'package:flutter/material.dart';

import '../Model/roomModel.dart';
import '../Repositories/user_repository.dart';

// Each dialog returns the entered value, or null when cancelled.

/// Asks for a room name; [room] is null when adding a new room.
Future<String?> showRoomNameDialog(BuildContext context, {RoomModel? room}) {
  final controller = TextEditingController(text: room?.name);
  return showDialog<String>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text(room == null ? 'Add room' : 'Rename ${room.name}'),
      content: TextField(
        controller: controller,
        autofocus: true,
        textCapitalization: TextCapitalization.sentences,
        decoration: const InputDecoration(labelText: 'Name'),
        onSubmitted: (value) => Navigator.pop(context, value),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, controller.text),
          child: Text(room == null ? 'Add' : 'Save'),
        ),
      ],
    ),
  );
}

Future<bool> showDeleteRoomDialog(BuildContext context, RoomModel room) async {
  final confirmed = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Delete room'),
      content: Text(
        'Delete ${room.name}? Its notes and staff assignments are deleted too. This cannot be undone.',
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context, false),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, true),
          child: const Text('Delete'),
        ),
      ],
    ),
  );
  return confirmed == true;
}

/// Lets the admin tick which of [staff] are designated to [room]; returns the
/// ticked user ids.
Future<Set<int>?> showAssignRoomDialog(
  BuildContext context,
  RoomModel room,
  List<AdminUser> staff,
  Set<int> assigned,
) {
  final selected = {...assigned};
  return showDialog<Set<int>>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('Staff for ${room.name}'),
      content: SizedBox(
        width: double.maxFinite,
        child: staff.isEmpty
            ? const Text('No staff accounts yet.')
            : StatefulBuilder(
                builder: (context, setState) => ListView(
                  shrinkWrap: true,
                  children: [
                    for (final user in staff)
                      CheckboxListTile(
                        contentPadding: EdgeInsets.zero,
                        title: Text(user.fullName),
                        subtitle: Text(user.role),
                        value: selected.contains(user.id),
                        onChanged: (value) => setState(
                          () => value!
                              ? selected.add(user.id)
                              : selected.remove(user.id),
                        ),
                      ),
                  ],
                ),
              ),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, selected),
          child: const Text('Save'),
        ),
      ],
    ),
  );
}
