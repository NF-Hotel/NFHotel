import 'package:flutter/foundation.dart';

import '../Repositories/user_repository.dart';

/// Holds the user list for the administration panel. Every action reloads the
/// list on success and throws [UserRepositoryException] on failure.
class AdminController extends ChangeNotifier {
  final _repo = UserRepository();

  List<AdminUser> users = [];
  bool loading = true;

  /// Room number -> ids of the staff designated to it.
  Map<int, Set<int>> assignments = {};

  Future<void> loadUsers() async {
    loading = true;
    notifyListeners();
    try {
      users = await _repo.fetchUsers();
      assignments = await _repo.fetchRoomAssignments();
    } finally {
      loading = false;
      notifyListeners();
    }
  }

  Future<void> createAccount(
    String firstName,
    String lastName,
    String email,
    String password,
  ) async {
    await _repo.register(firstName, lastName, email, password);
    await loadUsers();
  }

  Future<void> editName(AdminUser user, String firstName, String lastName) async {
    if (firstName.trim() == user.firstName && lastName.trim() == user.lastName) {
      return;
    }
    await _repo.updateName(user.id, firstName, lastName);
    await loadUsers();
  }

  Future<void> editEmail(AdminUser user, String email) async {
    if (email == user.email) return;
    await _repo.updateEmail(user.id, email);
    await loadUsers();
  }

  Future<void> editPassword(AdminUser user, String password) async {
    if (password.isEmpty) return;
    await _repo.updatePassword(user.id, password);
  }

  Future<void> editRole(AdminUser user, String role) async {
    if (role == user.role) return;
    await _repo.updateRole(user.id, role);
    await loadUsers();
  }

  List<AdminUser> staffForRoom(int room) => users
      .where((u) => assignments[room]?.contains(u.id) ?? false)
      .toList();

  Future<void> assignRoom(int room, Set<int> userIds) async {
    await _repo.updateRoomAssignments(room, userIds);
    assignments = await _repo.fetchRoomAssignments();
    notifyListeners();
  }

  Future<void> deleteUser(AdminUser user) async {
    await _repo.deleteUser(user.id);
    await loadUsers();
  }
}
