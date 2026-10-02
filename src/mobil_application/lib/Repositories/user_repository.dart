import 'dart:convert';

import 'package:http/http.dart' as http;

import '../Authentication/auth_session.dart';

const _apiBase = 'http://127.0.0.1:5142';

class AdminUser {
  final int id;
  final String firstName;
  final String lastName;
  final String email;
  final String role;

  AdminUser({
    required this.id,
    required this.firstName,
    required this.lastName,
    required this.email,
    required this.role,
  });

  String get fullName => '$firstName $lastName';

  factory AdminUser.fromJson(Map<String, dynamic> json) => AdminUser(
        id: json['id'] as int,
        firstName: json['firstName'] as String,
        lastName: json['lastName'] as String,
        email: json['email'] as String,
        role: json['role'] as String,
      );
}

/// Thrown by [UserRepository] when the API rejects a request; [message] is
/// safe to show to the user as-is.
class UserRepositoryException implements Exception {
  final String message;
  UserRepositoryException(this.message);

  @override
  String toString() => message;
}

class UserRepository {
  Map<String, String> get _authHeaders => {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer ${AuthSession.token}',
      };

  void _ensureOk(
    http.Response res,
    String message, {
    String? conflictMessage,
  }) {
    if (res.statusCode == 200) return;
    if (res.statusCode == 409 && conflictMessage != null) {
      throw UserRepositoryException(conflictMessage);
    }
    throw UserRepositoryException(message);
  }

  Future<void> register(
    String firstName,
    String lastName,
    String email,
    String password,
  ) async {
    final res = await http.post(
      Uri.parse('$_apiBase/auth/register'),
      headers: _authHeaders,
      body: jsonEncode({
        'firstName': firstName.trim(),
        'lastName': lastName.trim(),
        'email': email,
        'password': password,
      }),
    );
    _ensureOk(
      res,
      'Could not create account',
      conflictMessage: 'Email already registered',
    );
  }

  Future<http.Response> updateMyName(String firstName, String lastName) =>
      http.put(
        Uri.parse('$_apiBase/me/name'),
        headers: _authHeaders,
        body: jsonEncode({'firstName': firstName, 'lastName': lastName}),
      );

  Future<List<AdminUser>> fetchUsers() async {
    final res =
        await http.get(Uri.parse('$_apiBase/admin/users'), headers: _authHeaders);
    _ensureOk(res, 'Could not load users (${res.statusCode})');
    return (jsonDecode(res.body) as List)
        .map((e) => AdminUser.fromJson(e))
        .toList();
  }

  Future<void> updateName(int id, String firstName, String lastName) async {
    firstName = firstName.trim();
    lastName = lastName.trim();
    if (firstName.isEmpty || lastName.isEmpty) {
      throw UserRepositoryException('First and last name are required');
    }
    final res = await http.put(
      Uri.parse('$_apiBase/admin/users/$id/name'),
      headers: _authHeaders,
      body: jsonEncode({'firstName': firstName, 'lastName': lastName}),
    );
    _ensureOk(res, 'Could not update name');
  }

  Future<void> updateEmail(int id, String email) async {
    final res = await http.put(
      Uri.parse('$_apiBase/admin/users/$id/email'),
      headers: _authHeaders,
      body: jsonEncode({'email': email}),
    );
    _ensureOk(
      res,
      'Could not update email',
      conflictMessage: 'Email already in use',
    );
  }

  Future<void> updatePassword(int id, String password) async {
    final res = await http.put(
      Uri.parse('$_apiBase/admin/users/$id/password'),
      headers: _authHeaders,
      body: jsonEncode({'password': password}),
    );
    _ensureOk(res, 'Could not update password');
  }

  Future<void> updateRole(int id, String role) async {
    final res = await http.put(
      Uri.parse('$_apiBase/admin/users/$id/role'),
      headers: _authHeaders,
      body: jsonEncode({'role': role}),
    );
    _ensureOk(res, 'Could not update role');
  }

  Future<void> deleteUser(int id) async {
    final res = await http.delete(
      Uri.parse('$_apiBase/admin/users/$id'),
      headers: _authHeaders,
    );
    _ensureOk(res, 'Could not delete account');
  }
}
