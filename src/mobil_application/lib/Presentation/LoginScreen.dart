import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import '../Authentication/auth_session.dart';
import 'package:flutter_login/flutter_login.dart';
import 'package:google_fonts/google_fonts.dart';

import '../api_config.dart';
import '../theme.dart';
import 'NameSetupScreen.dart';
import 'RoomList.dart';

//Fandt flutter_login https://pub.dev/packages/flutter_login#-installing-tab-

//login logic
class LoginScreen extends StatelessWidget {
  const LoginScreen({super.key});

  Future<String?> _authUser(LoginData data) async {
    try {
      final res = await http.post(
        Uri.parse('$apiBase/auth/login'),
        headers: {'Content-Type': 'application/json'},
        body: jsonEncode({'email': data.name, 'password': data.password}),
      );
      if (res.statusCode == 200) {
        AuthSession.token = jsonDecode(res.body)['accessToken'] as String;
        return null; // Login successful
      }
      if (res.statusCode == 401) {
        return 'Invalid username or password'; // Unauthorized
      }
      return 'An error occurred'; // Other error
    } catch (e) {
      return 'Could not reach server';
    }
  }

  @override
  Widget build(BuildContext context) {
    return FlutterLogin(
      title: 'NFHotel',
      onLogin: _authUser,
      onSubmitAnimationCompleted: () {
        Navigator.of(
          context,
        ).pushReplacement(
          MaterialPageRoute(
            builder: (context) => AuthSession.hasName
                ? const RoomOverview()
                : const NameSetupScreen(),
          ),
        );
      },
      hideForgotPasswordButton: true, // no recovery endpoint in LoginApi; admins reset passwords in the admin panel
      onRecoverPassword: (_) async => 'Password recovery is not available',
      theme: LoginTheme(
        primaryColor: AppColors.navy,
        accentColor: AppColors.amber,
        pageColorLight: AppColors.navy,
        pageColorDark: AppColors.navyDark,
        bodyStyle: GoogleFonts.inter(color: AppColors.textMuted),
        buttonStyle: GoogleFonts.inter(
          fontWeight: FontWeight.w600,
          color: AppColors.navyDark,
        ),
        cardTheme: CardTheme(
          color: Colors.white,
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
        buttonTheme: LoginButtonTheme(
          backgroundColor: AppColors.amber,
          elevation: 0,
          highlightElevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
        ),
      ),
    );
  }
}
