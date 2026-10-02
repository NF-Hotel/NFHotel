import 'package:flutter/material.dart';

import '../Controllers/adminController.dart';
import '../Repositories/user_repository.dart';
import '../Widgets/UserDialogs.dart';

class AdministrationPanel extends StatefulWidget {
  const AdministrationPanel({super.key});

  @override
  _AdministrationPanelState createState() => _AdministrationPanelState();
}

class _AdministrationPanelState extends State<AdministrationPanel> {
  final _admin = AdminController();
  final _formKey = GlobalKey<FormState>();
  final _firstNameController = TextEditingController();
  final _lastNameController = TextEditingController();
  final _emailController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _creating = false;

  @override
  void initState() {
    super.initState();
    _run(_admin.loadUsers);
  }

  @override
  void dispose() {
    _admin.dispose();
    _firstNameController.dispose();
    _lastNameController.dispose();
    _emailController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  /// Runs an action and shows its error as a snackbar. Returns whether it
  /// succeeded.
  Future<bool> _run(Future<void> Function() action) async {
    try {
      await action();
      return true;
    } on UserRepositoryException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(
          context,
        ).showSnackBar(SnackBar(content: Text(e.message)));
      }
      return false;
    }
  }

  Future<void> _createAccount() async {
    if (!_formKey.currentState!.validate()) return;

    setState(() => _creating = true);
    final ok = await _run(
      () => _admin.createAccount(
        _firstNameController.text,
        _lastNameController.text,
        _emailController.text,
        _passwordController.text,
      ),
    );
    if (!mounted) return;
    setState(() => _creating = false);

    if (ok) {
      _firstNameController.clear();
      _lastNameController.clear();
      _emailController.clear();
      _passwordController.clear();
    }
  }

  @override
  Widget build(BuildContext context) {
    final textTheme = Theme.of(context).textTheme;
    return Scaffold(
      appBar: AppBar(title: const Text('Administration')),
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            keyboardDismissBehavior: ScrollViewKeyboardDismissBehavior.onDrag,
            padding: const EdgeInsets.all(16),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 420),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('Create account', style: textTheme.headlineSmall),
                  const SizedBox(height: 8),
                  Text(
                    'Set up staff access to manage rooms.',
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
                              validator: (value) =>
                                  (value == null || value.trim().isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _lastNameController,
                              textCapitalization: TextCapitalization.words,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'Last name',
                              ),
                              validator: (value) =>
                                  (value == null || value.trim().isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _emailController,
                              keyboardType: TextInputType.emailAddress,
                              textInputAction: TextInputAction.next,
                              decoration: const InputDecoration(
                                labelText: 'Email',
                              ),
                              validator: (value) =>
                                  (value == null || value.isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 16),
                            TextFormField(
                              controller: _passwordController,
                              decoration: const InputDecoration(
                                labelText: 'Password',
                              ),
                              obscureText: true,
                              validator: (value) =>
                                  (value == null || value.isEmpty)
                                  ? 'Required'
                                  : null,
                            ),
                            const SizedBox(height: 24),
                            ElevatedButton(
                              onPressed: _creating ? null : _createAccount,
                              child: _creating
                                  ? const SizedBox(
                                      height: 20,
                                      width: 20,
                                      child: CircularProgressIndicator(
                                        strokeWidth: 2,
                                      ),
                                    )
                                  : const Text('Sign Up'),
                            ),
                          ],
                        ),
                      ),
                    ),
                  ),
                  const SizedBox(height: 32),
                  Text('Users', style: textTheme.headlineSmall),
                  const SizedBox(height: 8),
                  ListenableBuilder(
                    listenable: _admin,
                    builder: (context, _) => _buildUserList(),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildUserList() {
    if (_admin.loading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 24),
        child: Center(child: CircularProgressIndicator()),
      );
    }
    if (_admin.users.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 16),
        child: Text('No other users.'),
      );
    }
    return Card(
      child: ListView.separated(
        shrinkWrap: true,
        physics: const NeverScrollableScrollPhysics(),
        itemCount: _admin.users.length,
        separatorBuilder: (_, _) => const Divider(height: 1),
        itemBuilder: (_, i) {
          final user = _admin.users[i];
          return ListTile(
            title: Text(user.fullName),
            subtitle: Text('${user.email}\n${user.role}'),
            isThreeLine: true,
            // one menu instead of 4 icon buttons, which left no room for the email on phones
            trailing: PopupMenuButton<Future<void> Function()>(
              tooltip: 'Actions',
              onSelected: _run,
              itemBuilder: (_) => [
                PopupMenuItem(
                  value: () async {
                    final name = await showEditNameDialog(context, user);
                    if (name != null) {
                      await _admin.editName(user, name.$1, name.$2);
                    }
                  },
                  child: const ListTile(
                    leading: Icon(Icons.person),
                    title: Text('Edit name'),
                  ),
                ),
                PopupMenuItem(
                  value: () async {
                    final email = await showEditEmailDialog(context, user);
                    if (email != null) await _admin.editEmail(user, email);
                  },
                  child: const ListTile(
                    leading: Icon(Icons.edit),
                    title: Text('Edit email'),
                  ),
                ),
                PopupMenuItem(
                  value: () async {
                    final password = await showEditPasswordDialog(context, user);
                    if (password != null) {
                      await _admin.editPassword(user, password);
                    }
                  },
                  child: const ListTile(
                    leading: Icon(Icons.lock_reset),
                    title: Text('Change password'),
                  ),
                ),
                PopupMenuItem(
                  value: () async {
                    final role = await showEditRoleDialog(context, user);
                    if (role != null) await _admin.editRole(user, role);
                  },
                  child: const ListTile(
                    leading: Icon(Icons.badge),
                    title: Text('Change role'),
                  ),
                ),
                PopupMenuItem(
                  value: () async {
                    if (await showDeleteUserDialog(context, user)) {
                      await _admin.deleteUser(user);
                    }
                  },
                  child: const ListTile(
                    leading: Icon(Icons.delete, color: Colors.red),
                    title: Text('Delete account'),
                  ),
                ),
              ],
            ),
          );
        },
      ),
    );
  }
}
