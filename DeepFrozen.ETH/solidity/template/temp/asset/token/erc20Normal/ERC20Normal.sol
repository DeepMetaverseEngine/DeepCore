// SPDX-License-Identifier: MIT
pragma solidity ^0.8.0;

import '@openzeppelin/contracts/token/ERC20/ERC20.sol';
import "@openzeppelin/contracts/utils/math/SafeMath.sol";

import "./ERC20NormalInterface.sol";

contract ERC20Normal is ERC20, ERC20NormalInterface {

    using SafeMath for uint256;

    address internal _manager;

    constructor(
        string memory _name_,
        string memory _symbol_
    )
    ERC20(
        _name_,
        _symbol_
    ){
        _manager = msg.sender;
    }

    modifier onlyManager() {
        require(_manager == msg.sender, "onlyManager");
        _;
    }

    function mint(address account, uint256 amount) override external onlyManager {
        _mint(account, amount);
    }

    function burn(address account, uint256 amount) override external onlyManager {
        _burn(account, amount);
    }

    function manager() override public view returns (address) {
        return _manager;
    }

    function transferOwnership(address newOwner) override public onlyManager {
        require(newOwner != address(0), "new owner is the zero address");
        emit OwnershipTransferred(_manager, newOwner);
        _manager = newOwner;
    }

    function _beforeTokenTransfer(address from, address to, uint256 amount) virtual override(ERC20) internal {

        ERC20._beforeTokenTransfer(from, to, amount);
    }
}
