use std::fs;
use std::io;
use std::path::Path;
use std::process::Command;

use runrusttime::utils::{ciatools_root, make_executable, run_sibling_executable};

#[cfg(target_os = "macos")]
fn remove_quarantine(path: &Path) {
    let _ = Command::new("xattr")
        .args(["-d", "com.apple.quarantine", path.to_str().unwrap_or_default()])
        .status();
}

fn copy_required_file(src: &Path, dst: &Path) -> io::Result<()> {
    if !src.is_file() {
        return Err(io::Error::new(
            io::ErrorKind::NotFound,
            format!("source file not found: {}", src.display()),
        ));
    }

    if let Some(parent) = dst.parent() {
        fs::create_dir_all(parent)?;
    }

    fs::copy(src, dst)?;

    #[cfg(unix)]
    make_executable(dst)?;

    #[cfg(target_os = "macos")]
    remove_quarantine(dst);

    println!("copied: {} -> {}", src.display(), dst.display());
    Ok(())
}

fn main() -> io::Result<()> {
    let root_path = ciatools_root()?;
    let src_dir = root_path.join("builder_files_sources");
    let dst_dir = root_path.join("USER_FILES");

    println!("[import] CIAToolsR root = {}", root_path.display());

    if !src_dir.is_dir() {
        return Err(io::Error::new(
            io::ErrorKind::NotFound,
            format!("builder_files_sources not found: {}", src_dir.display()),
        ));
    }

    fs::create_dir_all(&dst_dir)?;

    let exe_ext = std::env::consts::EXE_SUFFIX;
    let script_ext = if cfg!(windows) { ".bat" } else { ".sh" };

    let build_script = format!("build{}", script_ext);
    copy_required_file(&src_dir.join(&build_script), &dst_dir.join(&build_script))?;

    for tool in ["makerom", "bannertool"] {
        let src_tool_name = if cfg!(target_os = "macos") {
            let mac_specific = format!("{}OSX", tool);
            if src_dir.join(&mac_specific).is_file() {
                mac_specific
            } else {
                format!("{}{}", tool, exe_ext)
            }
        } else {
            format!("{}{}", tool, exe_ext)
        };

        let dst_tool_name = format!("{}{}", tool, exe_ext);

        copy_required_file(&src_dir.join(&src_tool_name), &dst_dir.join(&dst_tool_name))?;
    }

    println!("[import] start compile");
    run_sibling_executable("compile", &root_path)?;

    Ok(())
}