use std::io;
use std::process::Command;

use runrusttime::utils::{ciatools_root, make_executable, run_sibling_executable};

fn main() -> io::Result<()> {
    let root_path = ciatools_root()?;
    let user_files = root_path.join("USER_FILES");

    println!("[compile] CIAToolsR root = {}", root_path.display());

    if !user_files.is_dir() {
        return Err(io::Error::new(
            io::ErrorKind::NotFound,
            format!("USER_FILES not found: {}", user_files.display()),
        ));
    }

    let build_script = if cfg!(windows) { "build.bat" } else { "build.sh" };
    let build_script_path = user_files.join(build_script);

    if !build_script_path.is_file() {
        return Err(io::Error::new(
            io::ErrorKind::NotFound,
            format!("build script not found: {}", build_script_path.display()),
        ));
    }

    #[cfg(unix)]
    make_executable(&build_script_path)?;

    let status = if cfg!(windows) {
        Command::new("cmd")
            .args(["/C", build_script])
            .current_dir(&user_files)
            .status()?
    } else {
        Command::new("bash")
            .arg(build_script)
            .current_dir(&user_files)
            .status()?
    };

    if !status.success() {
        return Err(io::Error::new(
            io::ErrorKind::Other,
            format!("build script failed with status: {}", status),
        ));
    }

    println!("[compile] start cleanup");
    run_sibling_executable("delete", &root_path)?;

    Ok(())
}
