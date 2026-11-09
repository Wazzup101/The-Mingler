# Linux release identity verification

The signing-only workflow verifies the existing beta.1 downloads without changing
them. Signed approval is established only by a successful workflow run and real
signature verification, not by the presence of these files.

The workflow `mingler-linux-release-attestation.yml` signs approval of the five
already published beta.1 assets only after verifying sizes and SHA-256 against a
reviewed, versioned manifest. It never extracts or runs those downloads, builds
an app, uploads release assets, or touches Windows or Mac releases. It uses
short-lived GitHub/Sigstore identity instead of a stored signing key. Its claim
is release approval, not proof that an older binary was built in this workflow,
native testing, vulnerability freedom, or independent identity proof.

Review workflow/verifier/manifest changes on main, then manually run the workflow
on main with its confirmation after the publisher authorizes signing.
Success requires all verification steps to pass and a real attestation URL.
No signature claim is valid merely because the draft or checksums exist.

Users can then verify a downloaded archive before running it:

```sh
gh attestation verify MinglerSync-1.1.0-beta.1-linux-x64.tar.gz \
  --repo Wazzup101/The-Mingler \
  --signer-workflow Wazzup101/The-Mingler/.github/workflows/mingler-linux-release-attestation.yml \
  --source-ref refs/heads/main --deny-self-hosted-runners \
  --predicate-type https://mingler.cc/attestations/release-approval/v1
```

Run the same check for `run-preview.sh` before executing it and for the remaining
three files. A failure or missing attestation means identity is not verified;
do not bypass it or substitute a checksum for a signature. For stronger pinning,
add `--source-digest` with the independently reviewed workflow commit.

Future binaries need a separately reviewed manifest; beta.1 approval does not
cover a security-patched candidate or include later source fixes. Build provenance
for a future app should come from its actual trusted build job after deciding
what source is appropriate to publish.

Account hardening still matters: use phishing-resistant MFA/passkeys, review who
can write main or edit releases, protect workflow/manifest changes with review,
use minimal token permissions, and consider immutable releases. These account
and repository settings were not changed or verified by this source update.
Signatures from a compromised trusted publisher do not solve account compromise.

References: [GitHub attest action](https://github.com/actions/attest),
[verification options](https://cli.github.com/manual/gh_attestation_verify).
