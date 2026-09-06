use std::fs;
use std::io;

use runrusttime::utils::ciatools_root;

fn main() -> io::Result<()> {
    let root_path = ciatools_root()?;
    let user_files = root_path.join("USER_FILES");

    println!("[delete] CIAToolsR root = {}", root_path.display());

    if !user_files.is_dir() {
        return Err(io::Error::new(
            io::ErrorKind::NotFound,
            format!("USER_FILES not found: {}", user_files.display()),
        ));
    }

    let exe_ext = std::env::consts::EXE_SUFFIX;
    let script_ext = if cfg!(windows) { ".bat" } else { ".sh" };

    let files = [
        format!("build{}", script_ext),
        format!("bannertool{}", exe_ext),
        format!("makerom{}", exe_ext),
    ];

    for file in files {
        let path = user_files.join(&file);

        if path.is_file() {
            fs::remove_file(&path)?;
            println!("deleted: {}", path.display());
        } else {
            println!("skip, not found: {}", path.display());
        }
    }

    println!("cleanup finished");
    Ok(())
}
