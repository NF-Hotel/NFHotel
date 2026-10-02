import 'package:flutter/material.dart';

import '../Model/Enums/UserRole.dart';
import '../Repositories/user_repository.dart';

// Each dialog returns the entered value, or null when cancelled.

Future<(String, String)?> showEditNameDialog(
  BuildContext context,
  AdminUser user,
) async {
  final firstController = TextEditingController(text: user.firstName);
  final lastController = TextEditingController(text: user.lastName);
  final saved = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Edit name'),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          TextField(
            controller: firstController,
            textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(labelText: 'First name'),
          ),
          const SizedBox(height: 16),
          TextField(
            controller: lastController,
            textCapitalization: TextCapitalization.words,
            decoration: const InputDecoration(labelText: 'Last name'),
          ),
        ],
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context, false),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, true),
          child: const Text('Save'),
        ),
      ],
    ),
  );
  if (saved != true) return null;
  return (firstController.text, lastController.text);
}

Future<String?> showEditEmailDialog(BuildContext context, AdminUser user) {
  final controller = TextEditingController(text: user.email);
  return showDialog<String>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Edit email'),
      content: TextField(
        controller: controller,
        keyboardType: TextInputType.emailAddress,
        decoration: const InputDecoration(labelText: 'Email'),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, controller.text),
          child: const Text('Save'),
        ),
      ],
    ),
  );
}

Future<String?> showEditPasswordDialog(BuildContext context, AdminUser user) {
  final controller = TextEditingController();
  return showDialog<String>(
    context: context,
    builder: (context) => AlertDialog(
      title: Text('Change password for ${user.email}'),
      content: TextField(
        controller: controller,
        obscureText: true,
        decoration: const InputDecoration(labelText: 'New password'),
      ),
      actions: [
        TextButton(
          onPressed: () => Navigator.pop(context),
          child: const Text('Cancel'),
        ),
        TextButton(
          onPressed: () => Navigator.pop(context, controller.text),
          child: const Text('Save'),
        ),
      ],
    ),
  );
}

Future<String?> showEditRoleDialog(BuildContext context, AdminUser user) {
  return showDialog<String>(
    context: context,
    builder: (context) => SimpleDialog(
      title: Text('Change role for ${user.email}'),
      children: UserRole.values
          .map(
            (role) => SimpleDialogOption(
              onPressed: () => Navigator.pop(context, role.name),
              child: Text(role.name),
            ),
          )
          .toList(),
    ),
  );
}

Future<bool> showDeleteUserDialog(BuildContext context, AdminUser user) async {
  final confirmed = await showDialog<bool>(
    context: context,
    builder: (context) => AlertDialog(
      title: const Text('Delete account'),
      content: Text('Delete ${user.email}? This cannot be undone.'),
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
