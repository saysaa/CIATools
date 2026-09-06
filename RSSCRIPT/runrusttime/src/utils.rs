use std::env;
use std::fs;
use std::io;
use std::path::{Path, PathBuf};
use std::process::Command;

pub fn ciatools_root() -> io::Result<PathBuf> {
    let exe_path = env::current_exe()?;
    let exe_dir = exe_path
        .parent()
        .ok_or_else(|| io::Error::new(io::ErrorKind::Other, "executable directory not found"))?
        .to_path_buf();

    if looks_like_app_root(&exe_dir) {
        return Ok(exe_dir);
    }

    let current_dir = env::current_dir()?;
    if looks_like_app_root(&current_dir) {
        return Ok(current_dir);
    }

    let mut dir = exe_dir.clone();
    loop {
        if looks_like_app_root(&dir) {
            return Ok(dir);
        }

        if !dir.pop() {
            break;
        }
    }

    Err(io::Error::new(
        io::ErrorKind::NotFound,
        format!(
            "CIAToolsR root not found from executable path: {}",
            exe_path.display()
        ),
    ))
}

fn looks_like_app_root(path: &Path) -> bool {
    path.join("root_path").is_file()
        || path.join("RSSCRIPT").is_dir()
        || path.join("USER_FILES").is_dir()
        || path.join("builder_files_sources").is_dir()
}

pub fn executable_name(name: &str) -> String {
    format!("{}{}", name, env::consts::EXE_SUFFIX)
}

pub fn sibling_executable(name: &str) -> io::Result<PathBuf> {
    let exe_path = env::current_exe()?;
    let exe_dir = exe_path
        .parent()
        .ok_or_else(|| io::Error::new(io::ErrorKind::Other, "executable directory not found"))?;

    Ok(exe_dir.join(executable_name(name)))
}

pub fn run_sibling_executable(name: &str, working_dir: &Path) -> io::Result<()> {
    let exe = sibling_executable(name)?;

    if !exe.is_file() {
        return Err(io::Error::new(
            io::ErrorKind::NotFound,
            format!("executable not found: {}", exe.display()),
        ));
    }

    #[cfg(unix)]
    make_executable(&exe)?;

    let status = Command::new(&exe).current_dir(working_dir).status()?;

    if !status.success() {
        return Err(io::Error::new(
            io::ErrorKind::Other,
            format!("{} failed with status: {}", exe.display(), status),
        ));
    }

    Ok(())
}

#[cfg(unix)]
pub fn make_executable(path: &Path) -> io::Result<()> {
    use std::os::unix::fs::PermissionsExt;

    let mut permissions = fs::metadata(path)?.permissions();
    permissions.set_mode(0o700);
    fs::set_permissions(path, permissions)
}

#[cfg(not(unix))]
pub fn make_executable(_path: &Path) -> io::Result<()> {
    Ok(())
}
