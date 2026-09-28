import 'dart:convert';

import 'package:flutter/material.dart';

import '../Authentication/auth_session.dart';
import '../Repositories/user_repository.dart';
import 'RoomList.dart';

/// Shown after login when the account has no name yet.
class NameSetupScreen extends StatefulWidget {
  const NameSetupScreen({super.key});

  @override
  State<NameSetupScreen> createState() => _NameSetupScreenState();
}

class _NameSetupScreenState extends State<NameSetupScreen> {
  final _repo = UserRepository();
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  bool _saving = false;

  @override
  void dispose() {
    _firstNameController.dispose();
    _lastNameController.dispose();
    super.dispose();
  }

  Future<void> _save() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _saving = true);
    final res = await _repo.updateMyName(
      _firstNameController.text.trim(),
      _lastNameController.text.trim(),
    );
    if (!mounted) return;
    setState(() => _saving = false);

    if (res.statusCode == 200) {
      AuthSession.token = jsonDecode(res.body)['accessToken'] as String;
    } else if (res.statusCode != 409) {
      // 409 means an admin already set the name, so just continue
      ScaffoldMessenger.of(
        context,
      ).showSnackBar(const SnackBar(content: Text('Could not save name')));
      return;
    }
    Navigator.of(context).pushReplacement(
      MaterialPageRoute(builder: (context) => const RoomOverview()),
    );
  }

  String? _required(String? value) =>
      (value == null || value.trim().isEmpty) ? 'Required' : null;

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Scaffold(
      appBar: AppBar(
        title: const Text('Your name'),
        automaticallyImplyLeading: false,
      ),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Welcome', style: textTheme.headlineSmall),
                  const SizedBox(height: 8),
                  Text(
                    'Add your name before continuing.',
                    style: textTheme.bodyMedium,
                  ),
                  const SizedBox(height: 24),
                  Card(
                    child: Padding(
                      padding: const EdgeInsets.all(20),
                      child: Form(
                        key: _formKey,
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.stretch,
                          children: [
                            TextFormField(
                              controller: _firstNameController,
                              textCapitalization: TextCapitalization.words,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'First name',
                              ),
                              validator: _required,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _lastNameController,
                              textCapitalization: TextCapitalization.words,
                              textInputAction: TextInputAction.done,
                              onFieldSubmitted: (_) => _save(),
                              decoration: const InputDecoration(
                                labelText: 'Last name',
                              ),
                              validator: _required,
                            ),
                            const SizedBox(height: 24),
                            ElevatedButton(
                              onPressed: _saving ? null : _save,
                              child: _saving
                                  ? const SizedBox(
                                      height: 20,
                                      width: 20,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : const Text('Continue'),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
