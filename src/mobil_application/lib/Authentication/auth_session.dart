import 'dart:convert';

class AuthSession {
  static String? token;

  static const _roleClaim =
      'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

  static Map<String, dynamic>? get _claims {
    final t = token;
    if (t == null) return null;
    final parts = t.split('.');
    if (parts.length != 3) return null;
    return jsonDecode(
      utf8.decode(base64Url.decode(base64Url.normalize(parts[1]))),
    );
  }

  static String? get role => _claims?[_roleClaim] as String?;

  static String? get email => _claims?['email'] as String?;

  static bool get hasName =>
      ((_claims?['given_name'] as String?) ?? '').trim().isNotEmpty &&
      ((_claims?['family_name'] as String?) ?? '').trim().isNotEmpty;

  // falls back to email for tokens issued before names were added
  static String get displayName {
    final name =
        '${_claims?['given_name'] ?? ''} ${_claims?['family_name'] ?? ''}'
            .trim();
    return name.isNotEmpty ? name : (email ?? '');
  }

  static bool get isAdmin => role == 'admin';
}
